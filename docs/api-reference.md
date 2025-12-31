# API 參考文件

本文件描述 Line 通知服務的 API 端點、請求格式和回應範例。

## 基礎資訊

- **基礎 URL**: `https://your-domain.com/api/v1`
- **認證方式**: JWT Token 或 API Key
- **回應格式**: JSON
- **字元編碼**: UTF-8

## 認證

### JWT Token 認證

在 HTTP 標頭中加入：
```
Authorization: Bearer {your_jwt_token}
```

### API Key 認證

在 HTTP 標頭中加入：
```
X-API-Key: {your_api_key}
```

---

## 認證 API

### Line Login

#### GET /auth/line/login

取得 Line Login 授權 URL。

**回應範例**:
```json
{
  "success": true,
  "data": {
    "authUrl": "https://access.line.me/oauth2/v2.1/authorize?..."
  }
}
```

#### POST /auth/line/callback

處理 Line Login 回調。

**請求**:
```json
{
  "code": "authorization_code_from_line",
  "state": "state_token"
}
```

**回應**:
```json
{
  "success": true,
  "data": {
    "accessToken": "jwt_token",
    "refreshToken": "refresh_token",
    "expiresIn": 604800,
    "user": {
      "id": 1,
      "lineUserId": "U1234567890",
      "displayName": "使用者名稱",
      "pictureUrl": "https://profile.line.me/...",
      "isActive": true
    }
  }
}
```

#### POST /auth/refresh

刷新 JWT Token。

**請求**:
```json
{
  "refreshToken": "your_refresh_token"
}
```

#### GET /auth/me

取得當前使用者資訊（需要認證）。

#### POST /auth/logout

登出當前使用者（需要認證）。

### 管理員認證

#### POST /auth/admin/login

管理員登入。

**請求**:
```json
{
  "username": "admin",
  "password": "password123"
}
```

#### POST /auth/admin/change-password

變更管理員密碼（需要管理員認證）。

---

## 訊息 API

### 發送訊息

#### POST /messages/send

發送告警訊息（需要 API Key 認證）。

**請求**:
```json
{
  "messageType": "ALERT",
  "title": "伺服器告警",
  "content": "CPU 使用率超過 90%",
  "priority": "high",
  "source": {
    "host": "web-server-01",
    "service": "nginx",
    "ip": "192.168.1.100"
  },
  "targetGroups": ["ops-team"],
  "metadata": {
    "alertId": "12345",
    "threshold": "90%"
  }
}
```

**回應**:
```json
{
  "success": true,
  "data": {
    "messageId": 123,
    "recipientCount": 5,
    "status": "queued"
  }
}
```

#### GET /messages/{id}

取得訊息詳情（需要認證）。

#### GET /messages

取得訊息列表（需要管理員認證）。

**查詢參數**:
- `page` - 頁碼（預設 1）
- `pageSize` - 每頁筆數（預設 20，最大 100）
- `messageType` - 訊息類型篩選
- `sourceHost` - 來源主機篩選
- `startDate` - 開始日期
- `endDate` - 結束日期

#### GET /messages/me

取得當前使用者的訊息列表（需要使用者認證）。

---

## 群組管理 API

所有群組管理 API 都需要管理員認證。

### GET /admin/groups

取得群組列表。

### POST /admin/groups

建立群組。

**請求**:
```json
{
  "name": "運維團隊",
  "code": "ops-team",
  "description": "負責系統運維的團隊",
  "hostFilter": "web-*,db-*",
  "serviceFilter": "nginx,mysql",
  "receiveTimeStart": "08:00",
  "receiveTimeEnd": "22:00",
  "muteTimeStart": "",
  "muteTimeEnd": "",
  "messageTypes": ["ALERT", "WARNING", "INFO"]
}
```

### GET /admin/groups/{id}

取得群組詳情。

### PUT /admin/groups/{id}

更新群組。

### DELETE /admin/groups/{id}

刪除群組。

### POST /admin/groups/{id}/members

新增群組成員。

**請求**:
```json
{
  "userIds": [1, 2, 3]
}
```

### DELETE /admin/groups/{id}/members

移除群組成員。

---

## 訊息類型 API

### GET /admin/message-types

取得所有訊息類型。

### POST /admin/message-types

建立訊息類型。

**請求**:
```json
{
  "code": "CRITICAL",
  "name": "嚴重告警",
  "description": "系統嚴重問題需要立即處理",
  "template": "[{messageType}] {title}\n{content}\n來源: {host}",
  "color": "#DC3545",
  "icon": "🔴",
  "isActive": true
}
```

### PUT /admin/message-types/{id}

更新訊息類型。

### DELETE /admin/message-types/{id}

刪除訊息類型。

---

## 使用者管理 API

### GET /admin/users

取得使用者列表（需要管理員認證）。

**查詢參數**:
- `page` - 頁碼
- `pageSize` - 每頁筆數
- `search` - 搜尋關鍵字
- `groupId` - 群組篩選
- `isActive` - 啟用狀態篩選

### PATCH /admin/users/{id}

更新使用者狀態（需要管理員認證）。

---

## API Key 管理 API

### GET /admin/api-keys

取得 API Key 列表。

### POST /admin/api-keys

建立新 API Key。

**請求**:
```json
{
  "name": "Nagios Integration",
  "description": "用於 Nagios 告警整合",
  "expiresAt": "2025-12-31T23:59:59Z",
  "allowedIps": ["192.168.1.0/24"]
}
```

**回應**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Nagios Integration",
    "key": "lnk_abc123...",
    "createdAt": "2025-01-01T00:00:00Z"
  },
  "message": "請妥善保存此 API Key，它不會再次顯示"
}
```

### DELETE /admin/api-keys/{id}

撤銷 API Key。

---

## 報表 API

### GET /admin/reports/summary

取得系統總覽。

**回應**:
```json
{
  "success": true,
  "data": {
    "totalUsers": 150,
    "activeUsers": 142,
    "totalGroups": 10,
    "totalMessagesToday": 523,
    "deliverySuccessRate": 98.5
  }
}
```

### GET /admin/reports/delivery-stats

取得發送統計。

**查詢參數**:
- `startDate` - 開始日期
- `endDate` - 結束日期
- `groupBy` - 分組方式（day, week, month）

---

## 審計日誌 API

### GET /admin/audit-logs

取得審計日誌（需要管理員認證）。

**查詢參數**:
- `page` - 頁碼
- `pageSize` - 每頁筆數
- `action` - 操作類型篩選
- `userId` - 使用者篩選
- `startDate` - 開始日期
- `endDate` - 結束日期

---

## 錯誤處理

所有 API 都會回傳統一的錯誤格式：

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "錯誤描述"
  }
}
```

### 常見錯誤碼

| 錯誤碼 | HTTP 狀態碼 | 描述 |
|--------|------------|------|
| UNAUTHORIZED | 401 | 未認證或 Token 過期 |
| FORBIDDEN | 403 | 無權限存取 |
| NOT_FOUND | 404 | 資源不存在 |
| VALIDATION_ERROR | 400 | 請求參數驗證失敗 |
| INVALID_API_KEY | 401 | API Key 無效或已撤銷 |
| RATE_LIMIT_EXCEEDED | 429 | 超過請求頻率限制 |
| INTERNAL_ERROR | 500 | 伺服器內部錯誤 |

---

## Rate Limiting

API 有頻率限制，預設為每分鐘 100 次請求。

超過限制時會回傳 HTTP 429 狀態碼，並在回應標頭中包含：

- `X-RateLimit-Limit`: 限制次數
- `X-RateLimit-Remaining`: 剩餘次數
- `X-RateLimit-Reset`: 重置時間（Unix 時間戳）
- `Retry-After`: 建議重試等待秒數
