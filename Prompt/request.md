# Line 訊息發送服務需求規格書

## 1. 專案概述

### 1.1 專案名稱
Line Notification Service (LNS) - Line 訊息通知服務

### 1.2 專案目標
建立一個以 Line 為主要發送管道的訊息通知服務，用於替代傳統 Nagios 簡訊發送服務，提供更即時、更經濟的告警通知方式。

### 1.3 專案範圍
- 使用者身份驗證與管理
- Line 訊息訂閱與發送
- 訊息類型管理（監控告警分類）
- API 介面供外部系統（如 Nagios）呼叫
- 訊息發送歷史記錄與查詢

---

## 2. 技術架構

### 2.1 技術堆疊
| 項目 | 技術選擇 | 說明 |
|------|----------|------|
| Web Server | IIS / Kestrel + Nginx | Windows 使用 IIS；macOS/Linux 使用 Kestrel + Nginx |
| 後端框架 | ASP.NET Core 10.0 | 跨平台、高效能，支援 Windows/macOS/Linux |
| 資料庫 | MySQL 8.0 (Docker) | 資料持久化，Docker 容器化部署 |
| 訊息服務 | Line Messaging API | 訊息發送管道（Line Notify 已於 2025/3 停止服務）|
| 容器化 | Podman / Docker | 完整容器化部署，Podman 與 Docker 相容 |
| 身份驗證 | JWT Token | API 認證機制 |

### 2.1.1 跨平台支援
| 開發/部署環境 | Web Server | 說明 |
|---------------|------------|------|
| Windows | IIS + ASP.NET Core Module | 企業環境首選 |
| macOS | Kestrel + Nginx (反向代理) | 開發環境 |
| Linux | Kestrel + Nginx (反向代理) | 生產環境 |
| Docker | Kestrel (內建) | 任何平台皆可，推薦方案 |

### 2.2 系統架構圖
```
┌─────────────────────────────────────────────────────────────────┐
│                        外部系統                                  │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐            │
│  │ Nagios  │  │ Zabbix  │  │ 其他監控 │  │ 自訂腳本 │            │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘            │
│       │            │            │            │                  │
│       └────────────┴─────┬──────┴────────────┘                  │
│                          ▼                                      │
│                   ┌──────────────┐                              │
│                   │   REST API   │                              │
│                   └──────┬───────┘                              │
└──────────────────────────┼──────────────────────────────────────┘
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Line Notification Service                      │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │                      IIS Web Server                       │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │    │
│  │  │ 使用者管理   │  │ 訂閱管理    │  │ 訊息發送    │       │    │
│  │  │ Controller  │  │ Controller  │  │ Controller  │       │    │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘       │    │
│  │         │                │                │              │    │
│  │         └────────────────┼────────────────┘              │    │
│  │                          ▼                               │    │
│  │                 ┌─────────────────┐                      │    │
│  │                 │  Service Layer  │                      │    │
│  │                 └────────┬────────┘                      │    │
│  │                          │                               │    │
│  │         ┌────────────────┼────────────────┐              │    │
│  │         ▼                ▼                ▼              │    │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐         │    │
│  │  │ Repository │  │ Line API   │  │ 佇列服務   │         │    │
│  │  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘         │    │
│  │        │               │               │                │    │
│  └────────┼───────────────┼───────────────┼────────────────┘    │
│           ▼               ▼               │                     │
│  ┌─────────────┐  ┌─────────────┐         │                     │
│  │   MySQL     │  │  Line API   │◄────────┘                     │
│  │  (Docker)   │  │  Server     │                               │
│  └─────────────┘  └─────────────┘                               │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. 功能需求

### 3.1 使用者管理模組

#### 3.1.1 Line 登入註冊（一鍵登入）
- **功能描述**：使用者透過 Line 帳號直接登入/註冊，無需傳統帳號密碼
- **流程**：
  1. 使用者點擊「使用 Line 登入」按鈕
  2. 導向 Line Login 授權頁面
  3. 使用者授權後，取得 Line User ID 與基本資料
  4. 系統檢查該 Line User ID 是否已存在
     - 若不存在：自動建立新帳號（首次登入即註冊）
     - 若已存在：直接登入
  5. 產生 JWT Token 回傳
- **取得資料**：
  - Line User ID（唯一識別）
  - 顯示名稱
  - 頭像 URL
  - Email（需額外申請權限，可選）
- **輸出**：JWT Token 或錯誤訊息

#### 3.1.2 Line Login OAuth 流程
```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  使用者   │     │   前端    │     │   後端   │     │ Line API │
└────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘
     │ 點擊登入      │                │                │
     │──────────────>│                │                │
     │               │ 導向 Line      │                │
     │<──────────────│ Authorization  │                │
     │               │                │                │
     │ 授權同意      │                │                │
     │─────────────────────────────────────────────────>│
     │               │                │                │
     │ Callback + Code                │                │
     │<─────────────────────────────────────────────────│
     │               │                │                │
     │──────────────>│ Code           │                │
     │               │───────────────>│ 交換 Token     │
     │               │                │───────────────>│
     │               │                │<───────────────│
     │               │                │ 取得使用者資料  │
     │               │                │───────────────>│
     │               │                │<───────────────│
     │               │  JWT Token     │                │
     │               │<───────────────│                │
     │  登入成功      │                │                │
     │<──────────────│                │                │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
```

#### 3.1.3 使用者資料管理
- 查看個人資料
- 更新顯示名稱（可覆蓋 Line 名稱）
- 查看所屬群組
- 登出

---

### 3.2 群組管理模組（後台管理）

#### 3.2.1 群組定義
群組用於分類使用者，決定使用者可接收哪些類型的訊息。

| 群組代碼 | 群組名稱 | 說明 | 可接收訊息類型 |
|----------|----------|------|----------------|
| INFRA | 基礎設施團隊 | 負責伺服器、網路 | CRITICAL, WARNING, OK |
| APP | 應用程式團隊 | 負責應用程式服務 | CRITICAL, WARNING, OK |
| DBA | 資料庫團隊 | 負責資料庫維運 | CRITICAL, WARNING, OK |
| MANAGER | 主管群組 | 接收重大告警 | CRITICAL |
| ALL | 全體人員 | 接收所有通知 | ALL |

#### 3.2.2 群組 CRUD（管理員）
- **新增群組**：建立新的使用者群組
- **修改群組**：修改群組名稱、說明、關聯的訊息類型
- **停用/啟用群組**：暫時停用群組的訊息接收
- **刪除群組**：刪除群組（需先移除所有成員）

#### 3.2.3 群組成員管理（管理員）
- **查看群組成員**：列出群組內所有使用者
- **加入成員**：將使用者加入群組（可批次操作）
- **移除成員**：將使用者從群組移除
- **使用者可屬於多個群組**

#### 3.2.4 群組訊息類型關聯
- 每個群組可關聯多個訊息類型
- 使用者加入群組後，自動訂閱該群組關聯的所有訊息類型
- 支援群組層級的篩選設定（來源主機、服務篩選）

---

### 3.3 後台管理模組（管理員專用）

#### 3.3.1 管理員功能清單
| 功能 | 說明 |
|------|------|
| 使用者管理 | 查看所有使用者、停用/啟用帳號、設定管理員權限 |
| 群組管理 | 群組 CRUD、成員管理、訊息類型關聯 |
| 訊息類型管理 | 訊息類型 CRUD |
| API Key 管理 | 建立/撤銷 API Key |
| 發送記錄 | 查看所有發送歷史、統計報表 |
| 系統設定 | Line API 設定、系統參數 |

#### 3.3.2 管理員權限設定
- **系統預設管理員帳號**：
  - 帳號：`ADMIN`
  - 密碼：`ADMIN`
  - 首次登入後強制要求修改密碼
- 預設管理員為超級管理員，可指定其他使用者為管理員
- 管理員可管理一般使用者，但不能修改其他管理員
- 一般使用者透過 Line Login 登入，管理員可額外使用帳號密碼登入後台

#### 3.3.3 後台介面
- 提供 Web 管理介面
- 支援 RWD 響應式設計
- 管理員登入後可存取後台功能

---

### 3.4 訊息類型管理模組

#### 3.4.1 訊息類型定義
系統預設的訊息類型（對應 Nagios 告警等級）：

| 類型代碼 | 類型名稱 | 說明 | 預設優先級 |
|----------|----------|------|------------|
| CRITICAL | 嚴重告警 | 系統嚴重故障、服務中斷 | 1 (最高) |
| WARNING | 警告 | 效能問題、即將超過閾值 | 2 |
| UNKNOWN | 未知狀態 | 監控狀態無法判定 | 3 |
| OK | 恢復正常 | 問題已解決、服務恢復 | 4 |
| INFO | 資訊通知 | 一般性通知、排程任務 | 5 (最低) |

#### 3.4.2 訊息類型 CRUD
- **新增類型**：管理員可新增自訂訊息類型
- **修改類型**：修改類型名稱、說明、優先級
- **停用/啟用類型**：暫時停用某類型的發送
- **刪除類型**：刪除自訂類型（系統預設類型不可刪除）

#### 3.4.3 訊息類型群組
- 支援建立類型群組（例如：「所有告警」= CRITICAL + WARNING + UNKNOWN）
- 使用者可訂閱群組而非單一類型

---

### 3.5 訂閱管理模組

#### 3.5.1 訂閱訊息類型
- **功能描述**：使用者選擇要接收的訊息類型
- **輸入**：
  - 訊息類型 ID（可多選）
  - 接收時段設定（可選）
  - 靜音時段設定（可選）
- **處理邏輯**：
  - 驗證使用者已綁定 Line
  - 儲存訂閱設定
- **輸出**：訂閱成功/失敗訊息

#### 3.5.2 訂閱設定選項
| 設定項目 | 說明 | 預設值 |
|----------|------|--------|
| 接收時段 | 指定接收通知的時間範圍 | 全天 (00:00-24:00) |
| 靜音時段 | 不接收通知的時間範圍 | 無 |
| 聚合發送 | 短時間內多則訊息聚合為一則 | 關閉 |
| 聚合間隔 | 聚合發送的時間間隔 | 5 分鐘 |
| 重複告警 | 相同告警是否重複發送 | 發送 |
| 重複間隔 | 重複告警的發送間隔 | 30 分鐘 |

#### 3.5.3 訂閱來源篩選
- 依來源主機 (Host) 篩選
- 依來源服務 (Service) 篩選
- 支援萬用字元匹配（例如：web-server-*）

---

### 3.6 訊息發送模組

#### 3.6.1 API 發送訊息
- **端點**：`POST /api/v1/messages/send`
- **認證**：API Key（Bearer Token）
- **說明**：訊息內容由呼叫端（前端/監控系統）提供，系統負責發送給訂閱該類型的使用者

##### 請求格式
```json
{
  "messageType": "CRITICAL",
  "title": "告警標題",
  "content": "告警內容",
  "source": {
    "host": "web-server-01",
    "service": "HTTP",
    "ip": "192.168.1.100"
  },
  "metadata": {
    "nagiosHost": "nagios.example.com",
    "eventId": "12345",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "targetGroups": ["INFRA", "DBA"],  // 可選，指定發送給特定群組
  "priority": "high"  // 可選：high, normal, low
}
```

##### 呼叫範例

**curl 範例**
```bash
# 發送嚴重告警
curl -X POST "https://your-lns-server/api/v1/messages/send" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "messageType": "CRITICAL",
    "title": "[CRITICAL] web-server-01 - HTTP Service Down",
    "content": "HTTP service is not responding. Connection refused on port 80.",
    "source": {
      "host": "web-server-01",
      "service": "HTTP",
      "ip": "192.168.1.100"
    },
    "priority": "high"
  }'
```

**PowerShell 範例**
```powershell
$headers = @{
    "Authorization" = "Bearer YOUR_API_KEY"
    "Content-Type" = "application/json"
}

$body = @{
    messageType = "WARNING"
    title = "[WARNING] db-server-01 - High CPU Usage"
    content = "CPU usage is at 85%. Threshold is 80%."
    source = @{
        host = "db-server-01"
        service = "CPU"
        ip = "192.168.1.50"
    }
    targetGroups = @("DBA")
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-lns-server/api/v1/messages/send" `
    -Method POST -Headers $headers -Body $body
```

**Python 範例**
```python
import requests

api_url = "https://your-lns-server/api/v1/messages/send"
headers = {
    "Authorization": "Bearer YOUR_API_KEY",
    "Content-Type": "application/json"
}

payload = {
    "messageType": "OK",
    "title": "[OK] web-server-01 - HTTP Service Recovered",
    "content": "HTTP service has recovered and is responding normally.",
    "source": {
        "host": "web-server-01",
        "service": "HTTP",
        "ip": "192.168.1.100"
    }
}

response = requests.post(api_url, json=payload, headers=headers)
print(response.json())
```

**C# 範例**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY");

var payload = new
{
    messageType = "CRITICAL",
    title = "[CRITICAL] app-server-01 - Application Error",
    content = "Application crashed with OutOfMemoryException.",
    source = new
    {
        host = "app-server-01",
        service = "MyApp",
        ip = "192.168.1.200"
    },
    targetGroups = new[] { "APP" },
    priority = "high"
};

var response = await client.PostAsJsonAsync(
    "https://your-lns-server/api/v1/messages/send", payload);
var result = await response.Content.ReadAsStringAsync();
```

##### 回應格式
```json
// 成功
{
  "success": true,
  "messageId": "msg_20241215_001234",
  "recipientCount": 5,
  "message": "訊息已排入發送佇列"
}

// 失敗
{
  "success": false,
  "error": {
    "code": "INVALID_MESSAGE_TYPE",
    "message": "無效的訊息類型: CRITICALX"
  }
}
```

#### 3.6.2 訊息發送流程
```
1. 接收 API 請求
2. 驗證 API Key
3. 驗證訊息格式
4. 查詢訂閱該類型的使用者
5. 套用使用者的篩選條件
6. 檢查接收時段/靜音時段
7. 處理聚合邏輯
8. 將訊息放入發送佇列
9. 呼叫 Line API 發送
10. 記錄發送結果
11. 回傳 API 回應
```

#### 3.6.3 Line 訊息格式
使用 Flex Message 格式化告警訊息：

```
┌────────────────────────────────┐
│ 🔴 CRITICAL - 嚴重告警          │
├────────────────────────────────┤
│ 主機: web-server-01            │
│ 服務: HTTP                     │
│ IP: 192.168.1.100              │
├────────────────────────────────┤
│ HTTP service is down           │
│                                │
│ 時間: 2024-01-15 10:30:00      │
├────────────────────────────────┤
│ [查看詳情]  [確認告警]          │
└────────────────────────────────┘
```

#### 3.6.4 發送重試機制
- 失敗自動重試 3 次
- 重試間隔：1秒、5秒、30秒
- 超過重試次數標記為失敗

---

### 3.7 訊息歷史與報表模組

#### 3.7.1 訊息歷史查詢
- 依時間範圍查詢
- 依訊息類型篩選
- 依發送狀態篩選（成功/失敗/待發送）
- 依來源主機/服務篩選

#### 3.7.2 統計報表
- 每日/週/月發送統計
- 各類型訊息分布圖
- 發送成功率統計
- 尖峰時段分析

---

## 4. API 規格

### 4.1 API 端點清單

#### 認證相關（Line Login）
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/auth/line | 取得 Line Login URL |
| GET | /api/v1/auth/line/callback | Line Login 回調（登入/註冊）|
| POST | /api/v1/auth/logout | 使用者登出 |
| POST | /api/v1/auth/refresh | 刷新 Token |

#### 後台管理員認證
| 方法 | 端點 | 說明 |
|------|------|------|
| POST | /api/v1/admin/auth/login | 管理員帳號密碼登入 |
| POST | /api/v1/admin/auth/change-password | 修改管理員密碼 |
| POST | /api/v1/admin/auth/logout | 管理員登出 |

#### 使用者相關
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/users/me | 取得當前使用者資料 |
| PUT | /api/v1/users/me | 更新使用者資料 |
| GET | /api/v1/users/me/groups | 取得我的群組清單 |
| GET | /api/v1/users/me/subscriptions | 取得我的訂閱清單 |

#### 群組管理（管理員）
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/admin/groups | 取得所有群組 |
| POST | /api/v1/admin/groups | 新增群組 |
| GET | /api/v1/admin/groups/{id} | 取得群組詳情 |
| PUT | /api/v1/admin/groups/{id} | 更新群組 |
| DELETE | /api/v1/admin/groups/{id} | 刪除群組 |
| GET | /api/v1/admin/groups/{id}/members | 取得群組成員 |
| POST | /api/v1/admin/groups/{id}/members | 加入成員 |
| DELETE | /api/v1/admin/groups/{id}/members/{userId} | 移除成員 |
| PUT | /api/v1/admin/groups/{id}/message-types | 設定群組可接收的訊息類型 |

#### 使用者管理（管理員）
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/admin/users | 取得所有使用者 |
| GET | /api/v1/admin/users/{id} | 取得使用者詳情 |
| PUT | /api/v1/admin/users/{id} | 更新使用者（停用/啟用/設管理員）|
| GET | /api/v1/admin/users/{id}/groups | 取得使用者所屬群組 |
| PUT | /api/v1/admin/users/{id}/groups | 設定使用者所屬群組（批次）|

#### 訂閱相關
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/subscriptions | 取得訂閱清單 |
| POST | /api/v1/subscriptions | 新增訂閱 |
| PUT | /api/v1/subscriptions/{id} | 更新訂閱設定 |
| DELETE | /api/v1/subscriptions/{id} | 取消訂閱 |

#### 訊息類型相關
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/message-types | 取得所有訊息類型 |
| POST | /api/v1/message-types | 新增訊息類型（管理員）|
| PUT | /api/v1/message-types/{id} | 更新訊息類型（管理員）|
| DELETE | /api/v1/message-types/{id} | 刪除訊息類型（管理員）|

#### 訊息發送相關
| 方法 | 端點 | 說明 |
|------|------|------|
| POST | /api/v1/messages/send | 發送訊息 |
| GET | /api/v1/messages | 查詢訊息歷史 |
| GET | /api/v1/messages/{id} | 取得訊息詳情 |
| POST | /api/v1/messages/test | 發送測試訊息 |

#### API Key 管理
| 方法 | 端點 | 說明 |
|------|------|------|
| GET | /api/v1/api-keys | 取得 API Key 清單 |
| POST | /api/v1/api-keys | 建立新 API Key |
| DELETE | /api/v1/api-keys/{id} | 刪除 API Key |

---

## 5. 資料庫設計

### 5.1 資料表結構

#### users（使用者）
```sql
CREATE TABLE users (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    line_user_id VARCHAR(50) NOT NULL UNIQUE COMMENT 'Line User ID，由 Line Login 取得',
    display_name VARCHAR(100) COMMENT '顯示名稱',
    picture_url VARCHAR(500) COMMENT 'Line 頭像 URL',
    email VARCHAR(100) COMMENT 'Line Email（需申請權限）',
    is_active BOOLEAN DEFAULT TRUE COMMENT '帳號是否啟用',
    is_admin BOOLEAN DEFAULT FALSE COMMENT '是否為管理員',
    is_super_admin BOOLEAN DEFAULT FALSE COMMENT '是否為超級管理員',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at DATETIME,
    INDEX idx_line_user_id (line_user_id),
    INDEX idx_is_active (is_active)
);
```

#### admins（管理員帳號）
```sql
CREATE TABLE admins (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE COMMENT '管理員帳號',
    password_hash VARCHAR(255) NOT NULL COMMENT '密碼雜湊 (BCrypt)',
    display_name VARCHAR(100) COMMENT '顯示名稱',
    is_super_admin BOOLEAN DEFAULT FALSE COMMENT '是否為超級管理員',
    is_active BOOLEAN DEFAULT TRUE COMMENT '帳號是否啟用',
    force_change_password BOOLEAN DEFAULT TRUE COMMENT '是否強制修改密碼',
    linked_user_id BIGINT COMMENT '關聯的 Line 使用者 ID（可選）',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at DATETIME,
    FOREIGN KEY (linked_user_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_username (username)
);

-- 預設管理員帳號（初始化資料）
INSERT INTO admins (username, password_hash, display_name, is_super_admin, force_change_password)
VALUES ('ADMIN', '$2a$12$...', '系統管理員', TRUE, TRUE);
-- 密碼為 ADMIN 的 BCrypt 雜湊
```

#### groups（群組）
```sql
CREATE TABLE `groups` (
    id INT PRIMARY KEY AUTO_INCREMENT,
    code VARCHAR(20) NOT NULL UNIQUE COMMENT '群組代碼',
    name VARCHAR(50) NOT NULL COMMENT '群組名稱',
    description TEXT COMMENT '群組說明',
    host_filter VARCHAR(255) COMMENT '來源主機篩選（支援萬用字元）',
    service_filter VARCHAR(255) COMMENT '來源服務篩選（支援萬用字元）',
    is_active BOOLEAN DEFAULT TRUE COMMENT '是否啟用',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (code),
    INDEX idx_is_active (is_active)
);
```

#### user_groups（使用者-群組關聯）
```sql
CREATE TABLE user_groups (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    group_id INT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT COMMENT '由哪個管理員加入',
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (group_id) REFERENCES `groups`(id) ON DELETE CASCADE,
    FOREIGN KEY (created_by) REFERENCES users(id),
    UNIQUE KEY uk_user_group (user_id, group_id),
    INDEX idx_user_id (user_id),
    INDEX idx_group_id (group_id)
);
```

#### group_message_types（群組-訊息類型關聯）
```sql
CREATE TABLE group_message_types (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    group_id INT NOT NULL,
    message_type_id INT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (group_id) REFERENCES `groups`(id) ON DELETE CASCADE,
    FOREIGN KEY (message_type_id) REFERENCES message_types(id) ON DELETE CASCADE,
    UNIQUE KEY uk_group_message_type (group_id, message_type_id)
);
```

#### message_types（訊息類型）
```sql
CREATE TABLE message_types (
    id INT PRIMARY KEY AUTO_INCREMENT,
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    priority INT DEFAULT 5,
    color VARCHAR(7) DEFAULT '#808080',
    icon VARCHAR(10),
    is_system BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### subscriptions（訂閱）
```sql
CREATE TABLE subscriptions (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    group_id INT COMMENT '透過群組訂閱時記錄',
    message_type_id INT NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    receive_start_time TIME DEFAULT '00:00:00' COMMENT '接收開始時間',
    receive_end_time TIME DEFAULT '23:59:59' COMMENT '接收結束時間',
    mute_start_time TIME COMMENT '靜音開始時間',
    mute_end_time TIME COMMENT '靜音結束時間',
    enable_aggregation BOOLEAN DEFAULT FALSE COMMENT '是否啟用聚合發送',
    aggregation_interval INT DEFAULT 5 COMMENT '聚合間隔（分鐘）',
    enable_repeat BOOLEAN DEFAULT TRUE COMMENT '是否接收重複告警',
    repeat_interval INT DEFAULT 30 COMMENT '重複告警間隔（分鐘）',
    host_filter VARCHAR(255) COMMENT '來源主機篩選（覆寫群組設定）',
    service_filter VARCHAR(255) COMMENT '來源服務篩選（覆寫群組設定）',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (group_id) REFERENCES `groups`(id) ON DELETE SET NULL,
    FOREIGN KEY (message_type_id) REFERENCES message_types(id),
    UNIQUE KEY uk_user_type (user_id, message_type_id),
    INDEX idx_user_id (user_id),
    INDEX idx_group_id (group_id)
);
```

#### messages（訊息）
```sql
CREATE TABLE messages (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    message_type_id INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    source_host VARCHAR(100),
    source_service VARCHAR(100),
    source_ip VARCHAR(45),
    metadata JSON,
    priority ENUM('high', 'normal', 'low') DEFAULT 'normal',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (message_type_id) REFERENCES message_types(id),
    INDEX idx_created_at (created_at),
    INDEX idx_source_host (source_host),
    INDEX idx_message_type (message_type_id)
);
```

#### message_deliveries（訊息發送記錄）
```sql
CREATE TABLE message_deliveries (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    message_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    status ENUM('pending', 'sent', 'failed', 'skipped') DEFAULT 'pending',
    line_message_id VARCHAR(100),
    sent_at DATETIME,
    error_message TEXT,
    retry_count INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_status (status),
    INDEX idx_user_id (user_id),
    INDEX idx_message_id (message_id)
);
```

#### api_keys（API 金鑰）
```sql
CREATE TABLE api_keys (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    name VARCHAR(100) NOT NULL,
    key_hash VARCHAR(255) NOT NULL,
    key_prefix VARCHAR(10) NOT NULL,
    permissions JSON,
    is_active BOOLEAN DEFAULT TRUE,
    expires_at DATETIME,
    last_used_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_key_prefix (key_prefix)
);
```

#### login_logs（登入記錄）
```sql
CREATE TABLE login_logs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    login_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    success BOOLEAN,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_login_at (login_at)
);
```

---

## 6. 非功能需求

### 6.1 效能需求
| 項目 | 需求 |
|------|------|
| API 回應時間 | < 500ms (P95) |
| 訊息發送延遲 | < 3 秒（從 API 呼叫到使用者收到）|
| 並發處理 | 支援 100 concurrent requests |
| 每日訊息量 | 支援 10,000 則/日 |

### 6.2 可用性需求
| 項目 | 需求 |
|------|------|
| 系統可用性 | 99.5% (允許每月約 3.6 小時停機) |
| 資料備份 | 每日自動備份 |
| 備份保留 | 保留 30 天 |

### 6.3 安全性需求
- 所有 API 通訊使用 HTTPS
- 使用 Line Login OAuth 2.0 認證，無需儲存使用者密碼
- API Key 使用 SHA-256 雜湊儲存
- 實作 Rate Limiting（100 requests/minute/IP）
- 實作 JWT Token 黑名單機制
- 記錄所有敏感操作（審計日誌）

### 6.4 相容性需求
- 支援 Nagios 原生 notification command 格式
- 提供 Nagios 整合腳本
- 支援 Zabbix webhook 格式
- 提供 curl 範例命令

---

## 7. 整合方式

### 7.1 Nagios 整合範例

#### notification command 設定
```bash
# /etc/nagios/objects/commands.cfg

define command {
    command_name    notify-host-by-line
    command_line    /usr/local/bin/send_line_notification.sh \
                    --type "$NOTIFICATIONTYPE$" \
                    --host "$HOSTNAME$" \
                    --hostaddress "$HOSTADDRESS$" \
                    --state "$HOSTSTATE$" \
                    --output "$HOSTOUTPUT$"
}

define command {
    command_name    notify-service-by-line
    command_line    /usr/local/bin/send_line_notification.sh \
                    --type "$NOTIFICATIONTYPE$" \
                    --host "$HOSTNAME$" \
                    --hostaddress "$HOSTADDRESS$" \
                    --service "$SERVICEDESC$" \
                    --state "$SERVICESTATE$" \
                    --output "$SERVICEOUTPUT$"
}
```

#### 發送腳本範例
```bash
#!/bin/bash
# /usr/local/bin/send_line_notification.sh

API_URL="https://your-lns-server/api/v1/messages/send"
API_KEY="your-api-key-here"

# 解析參數...

curl -X POST "$API_URL" \
  -H "Authorization: Bearer $API_KEY" \
  -H "Content-Type: application/json" \
  -d "{
    \"messageType\": \"$TYPE\",
    \"title\": \"[$STATE] $HOST - $SERVICE\",
    \"content\": \"$OUTPUT\",
    \"source\": {
      \"host\": \"$HOST\",
      \"service\": \"$SERVICE\",
      \"ip\": \"$HOSTADDRESS\"
    }
  }"
```

---

## 8. 部署架構

### 8.1 部署選項總覽
| 部署方式 | 適用環境 | 複雜度 | 推薦程度 |
|----------|----------|--------|----------|
| Podman / Podman Compose | 任何平台 | 低 | ⭐⭐⭐⭐⭐ 最推薦 |
| Docker / Docker Compose | 任何平台 | 低 | ⭐⭐⭐⭐⭐ 最推薦 |
| IIS | Windows Server | 中 | ⭐⭐⭐⭐ 企業環境 |
| Kestrel + Nginx | macOS / Linux | 中 | ⭐⭐⭐⭐ 開發/生產 |
| Kubernetes | 大規模部署 | 高 | ⭐⭐⭐ 進階需求 |

> **Podman 相容性說明**：本專案完全相容 Podman，所有 `docker` 命令可直接替換為 `podman`，`docker-compose` 可使用 `podman-compose` 或 `podman compose`。

### 8.2 Windows (IIS) 部署步驟
1. 安裝 .NET 10.0 Hosting Bundle
2. 建立 IIS 網站，指向發布目錄
3. 設定應用程式集區（No Managed Code）
4. 設定 HTTPS 憑證
5. 設定環境變數或 appsettings.json

### 8.3 macOS / Linux 部署步驟
1. 安裝 .NET 10.0 SDK/Runtime
2. 發布應用程式：`dotnet publish -c Release`
3. 設定 systemd 服務（Linux）或 launchd（macOS）
4. 安裝並設定 Nginx 反向代理
5. 設定 SSL 憑證（Let's Encrypt 或自簽）

#### Nginx 反向代理設定範例
```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 8.4 Podman / Docker 完整部署（推薦）

> **Podman 使用方式**：
> - 將 `docker-compose` 替換為 `podman-compose` 或 `podman compose`
> - 所有 `docker` 命令替換為 `podman`
> - 範例：`podman-compose up -d`

```yaml
# compose.yaml (適用於 Podman Compose 和 Docker Compose)
version: '3.8'

services:
  # ASP.NET Core API 服務
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: lns-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
      - ConnectionStrings__DefaultConnection=Server=mysql;Database=line_notification;User=lns_user;Password=${MYSQL_PASSWORD}
      - Line__ChannelAccessToken=${LINE_CHANNEL_ACCESS_TOKEN}
      - Line__ChannelSecret=${LINE_CHANNEL_SECRET}
    ports:
      - "5000:5000"
    depends_on:
      - mysql
      - redis
    networks:
      - lns-network
    restart: unless-stopped

  # MySQL 資料庫
  mysql:
    image: mysql:8.0
    container_name: lns-mysql
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: line_notification
      MYSQL_USER: lns_user
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    volumes:
      - mysql_data:/var/lib/mysql
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "3306:3306"
    networks:
      - lns-network
    restart: unless-stopped

  # Redis 訊息佇列
  redis:
    image: redis:7-alpine
    container_name: lns-redis
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    networks:
      - lns-network
    restart: unless-stopped

  # Nginx 反向代理 (可選，用於 HTTPS)
  nginx:
    image: nginx:alpine
    container_name: lns-nginx
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./certs:/etc/nginx/certs
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - api
    networks:
      - lns-network
    restart: unless-stopped

volumes:
  mysql_data:
  redis_data:

networks:
  lns-network:
    driver: bridge
```

#### Dockerfile / Containerfile
> Podman 使用 `Containerfile`（與 Dockerfile 語法完全相同），也可直接使用 `Dockerfile`。

```dockerfile
# Dockerfile / Containerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["LineNotificationService.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LineNotificationService.dll"]
```

---

## 9. 專案里程碑

### Phase 1 - 基礎架構（第 1-2 週）
- [x] 專案初始化
- [ ] 資料庫建立
- [ ] 基本 API 架構
- [ ] 使用者認證功能

### Phase 2 - 核心功能（第 3-4 週）
- [ ] Line Login 整合
- [ ] Line Messaging API 整合
- [ ] 訊息類型管理
- [ ] 訂閱功能

### Phase 3 - 發送功能（第 5-6 週）
- [ ] 訊息發送 API
- [ ] 發送佇列處理
- [ ] 重試機制
- [ ] 訊息歷史記錄

### Phase 4 - 整合與優化（第 7-8 週）
- [ ] Nagios 整合腳本
- [ ] 管理介面
- [ ] 效能優化
- [ ] 文件撰寫

---

## 10. 待確認事項

1. **Line API 選擇**
   - [ ] Line Notify（簡單，但將於 2025/3 停止服務）
   - [ ] Line Messaging API（推薦，功能完整）

2. **使用者數量預估**
   - 預估使用者數：______ 人
   - 預估每日訊息量：______ 則

3. **高可用性需求**
   - [ ] 是否需要負載平衡？
   - [ ] 是否需要資料庫主從架構？

4. **其他整合需求**
   - [ ] 是否需要支援其他監控系統？（Zabbix、Prometheus 等）
   - [ ] 是否需要 Email 備援發送？

---

## 附錄

### A. 參考資源
- [Line Messaging API 文件](https://developers.line.biz/en/docs/messaging-api/)
- [Line Login 文件](https://developers.line.biz/en/docs/line-login/)
- [ASP.NET Core 文件](https://docs.microsoft.com/aspnet/core/)

### B. 名詞解釋
| 名詞 | 說明 |
|------|------|
| LNS | Line Notification Service，本專案名稱 |
| JWT | JSON Web Token，認證機制 |
| Flex Message | Line 的彈性訊息格式 |
| Rate Limiting | API 請求頻率限制 |
