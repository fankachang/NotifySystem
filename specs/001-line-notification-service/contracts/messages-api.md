# API Contract: Messages API

**版本**: v1  
**Base Path**: `/api/v1/messages`

## 發送訊息

### POST /api/v1/messages/send

發送告警訊息到指定的接收者。

**認證**: API Key (Bearer Token)

**請求**:

```http
POST /api/v1/messages/send HTTP/1.1
Host: notify.example.com
Authorization: Bearer {API_KEY}
Content-Type: application/json

{
  "messageType": "CRITICAL",
  "title": "服務故障警報",
  "content": "Web Server 01 HTTP 服務無回應",
  "source": {
    "host": "web-server-01",
    "service": "HTTP",
    "ip": "192.168.1.100"
  },
  "metadata": {
    "checkCommand": "check_http",
    "lastState": "OK",
    "duration": "5m"
  },
  "targetGroups": ["INFRA", "WEB-TEAM"],
  "priority": "high"
}
```

**請求欄位**:

| 欄位 | 類型 | 必填 | 說明 |
|------|------|------|------|
| messageType | string | ✓ | 訊息類型代碼 (CRITICAL, WARNING, etc.) |
| title | string | ✓ | 訊息標題 (最長 200 字元) |
| content | string | ✓ | 訊息內容 (最長 2000 字元) |
| source.host | string | - | 來源主機名稱 |
| source.service | string | - | 來源服務名稱 |
| source.ip | string | - | 來源 IP 位址 |
| metadata | object | - | 擴展資料 (任意 JSON) |
| targetGroups | string[] | - | 指定發送群組，空陣列表示所有訂閱者 |
| priority | string | - | 優先級: high, normal, low (預設: normal) |

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "data": {
    "messageId": 12345,
    "recipientCount": 15,
    "status": "queued"
  }
}
```

**錯誤回應**:

| HTTP Code | Error Code | 說明 |
|-----------|------------|------|
| 400 | INVALID_MESSAGE_TYPE | 無效的訊息類型 |
| 400 | INVALID_REQUEST | 請求格式錯誤 |
| 401 | UNAUTHORIZED | API Key 無效或已撤銷 |
| 403 | FORBIDDEN | API Key 權限不足 |
| 429 | RATE_LIMITED | 超過請求頻率限制 |
| 503 | SERVICE_UNAVAILABLE | 服務暫時無法使用 |

```json
{
  "success": false,
  "error": {
    "code": "INVALID_MESSAGE_TYPE",
    "message": "訊息類型 'CRITICALX' 不存在或已停用"
  }
}
```

---

## 查詢訊息狀態

### GET /api/v1/messages/{id}

查詢單則訊息的發送狀態。

**認證**: API Key 或 JWT Token

**回應**:

```json
{
  "success": true,
  "data": {
    "id": 12345,
    "messageType": "CRITICAL",
    "title": "服務故障警報",
    "status": "completed",
    "recipientCount": 15,
    "sentCount": 14,
    "failedCount": 1,
    "createdAt": "2025-12-31T10:30:00Z",
    "completedAt": "2025-12-31T10:30:02Z"
  }
}
```

---

## 查詢訊息歷史

### GET /api/v1/messages

查詢訊息發送歷史（分頁）。

**認證**: JWT Token（管理員）

**查詢參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| page | int | 頁碼 (預設: 1) |
| pageSize | int | 每頁筆數 (預設: 20, 最大: 100) |
| messageType | string | 篩選訊息類型 |
| sourceHost | string | 篩選來源主機 |
| sourceService | string | 篩選來源服務 |
| status | string | 篩選狀態 |
| startDate | datetime | 開始日期 |
| endDate | datetime | 結束日期 |

**回應**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 12345,
        "messageType": "CRITICAL",
        "title": "服務故障警報",
        "sourceHost": "web-server-01",
        "recipientCount": 15,
        "sentCount": 14,
        "failedCount": 1,
        "createdAt": "2025-12-31T10:30:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 150,
      "totalPages": 8
    }
  }
}
```

---

## 查詢我的訊息

### GET /api/v1/messages/me

查詢當前使用者收到的訊息歷史。

**認證**: JWT Token（使用者）

**查詢參數**: 同上

**回應**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 12345,
        "messageType": "CRITICAL",
        "title": "服務故障警報",
        "content": "Web Server 01 HTTP 服務無回應",
        "sourceHost": "web-server-01",
        "status": "sent",
        "sentAt": "2025-12-31T10:30:01Z"
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 50,
      "totalPages": 3
    }
  }
}
```
