# API Contract: Admin API

**版本**: v1  
**Base Path**: `/api/v1/admin`

所有 Admin API 都需要 JWT Token（管理員權限）認證。

---

## 使用者管理

### GET /api/v1/admin/users

查詢所有使用者列表。

**查詢參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| page | int | 頁碼 |
| pageSize | int | 每頁筆數 |
| search | string | 搜尋（名稱） |
| groupId | long | 篩選群組 |
| isActive | bool | 篩選狀態 |

**回應**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 123,
        "displayName": "王小明",
        "avatarUrl": "https://...",
        "isActive": true,
        "isAdmin": false,
        "groups": [
          { "id": 1, "code": "INFRA", "name": "基礎設施團隊" }
        ],
        "createdAt": "2025-12-01T00:00:00Z",
        "lastLoginAt": "2025-12-31T10:00:00Z"
      }
    ],
    "pagination": { ... }
  }
}
```

### PATCH /api/v1/admin/users/{id}

更新使用者資訊。

**請求**:

```json
{
  "displayName": "新名稱",
  "isActive": false,
  "isAdmin": true
}
```

---

## 群組管理

### GET /api/v1/admin/groups

查詢所有群組。

### POST /api/v1/admin/groups

建立新群組。

**請求**:

```json
{
  "code": "INFRA",
  "name": "基礎設施團隊",
  "description": "負責監控基礎設施的團隊",
  "messageTypeIds": [1, 2],
  "hostFilter": "web-server-*",
  "serviceFilter": null,
  "activeTimeStart": "00:00:00",
  "activeTimeEnd": "23:59:59",
  "muteTimeStart": null,
  "muteTimeEnd": null,
  "suppressDuplicate": true,
  "duplicateIntervalMinutes": 30
}
```

**回應**:

```json
{
  "success": true,
  "data": {
    "id": 1,
    "code": "INFRA",
    "name": "基礎設施團隊",
    "memberCount": 0,
    "createdAt": "2025-12-31T10:00:00Z"
  }
}
```

### GET /api/v1/admin/groups/{id}

取得群組詳細資訊。

### PUT /api/v1/admin/groups/{id}

更新群組設定。

### DELETE /api/v1/admin/groups/{id}

刪除群組（需先移除所有成員）。

---

### POST /api/v1/admin/groups/{id}/members

批次加入群組成員。

**請求**:

```json
{
  "userIds": [123, 456, 789]
}
```

### DELETE /api/v1/admin/groups/{id}/members

批次移除群組成員。

**請求**:

```json
{
  "userIds": [123, 456]
}
```

---

## 訊息類型管理

### GET /api/v1/admin/message-types

查詢所有訊息類型。

### POST /api/v1/admin/message-types

建立自訂訊息類型。

**請求**:

```json
{
  "code": "MAINTENANCE",
  "name": "維護通知",
  "description": "系統維護通知",
  "priority": 5,
  "color": "#00BFFF",
  "icon": "wrench"
}
```

### PUT /api/v1/admin/message-types/{id}

更新訊息類型。

### DELETE /api/v1/admin/message-types/{id}

刪除自訂訊息類型（系統預設類型不可刪除）。

---

## API Key 管理

### GET /api/v1/admin/api-keys

查詢所有 API Key。

**回應**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Nagios Production",
        "keyPrefix": "nk_abc12...",
        "isActive": true,
        "createdBy": "ADMIN",
        "createdAt": "2025-12-01T00:00:00Z",
        "lastUsedAt": "2025-12-31T10:00:00Z",
        "expiresAt": null
      }
    ]
  }
}
```

### POST /api/v1/admin/api-keys

建立新 API Key。

**請求**:

```json
{
  "name": "Nagios Production",
  "expiresAt": null
}
```

**回應**（Key 只會顯示這一次）:

```json
{
  "success": true,
  "data": {
    "id": 2,
    "name": "Nagios Production",
    "key": "nk_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz",
    "keyPrefix": "nk_abc12...",
    "createdAt": "2025-12-31T10:00:00Z"
  },
  "warning": "請立即複製此 API Key，關閉後將無法再次查看完整金鑰。"
}
```

### DELETE /api/v1/admin/api-keys/{id}

撤銷 API Key。

---

## 報表與統計

### GET /api/v1/admin/reports/summary

取得摘要統計。

**查詢參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| startDate | date | 開始日期 |
| endDate | date | 結束日期 |

**回應**:

```json
{
  "success": true,
  "data": {
    "totalMessages": 5000,
    "totalDeliveries": 75000,
    "successRate": 99.5,
    "byMessageType": [
      { "code": "CRITICAL", "count": 500 },
      { "code": "WARNING", "count": 2000 },
      { "code": "OK", "count": 2500 }
    ],
    "byDay": [
      { "date": "2025-12-25", "count": 150 },
      { "date": "2025-12-26", "count": 200 }
    ],
    "peakHours": [9, 10, 14, 15]
  }
}
```

### GET /api/v1/admin/reports/delivery-stats

取得發送統計詳情。

---

## 管理員帳號管理

### GET /api/v1/admin/admins

查詢所有管理員（需超級管理員權限）。

### POST /api/v1/admin/admins

建立新管理員（需超級管理員權限）。

**請求**:

```json
{
  "username": "operator1",
  "password": "TempPass123!",
  "displayName": "操作員一號",
  "isSuperAdmin": false
}
```

### PATCH /api/v1/admin/admins/{id}

更新管理員帳號。

### DELETE /api/v1/admin/admins/{id}

刪除管理員帳號。

---

## 審計日誌

### GET /api/v1/admin/audit-logs

查詢審計日誌。

**查詢參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| page | int | 頁碼 |
| pageSize | int | 每頁筆數 |
| adminId | long | 篩選操作者 |
| action | string | 篩選操作類型 |
| entityType | string | 篩選實體類型 |
| startDate | datetime | 開始日期 |
| endDate | datetime | 結束日期 |

**回應**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "admin": { "id": 1, "username": "ADMIN" },
        "action": "CREATE",
        "entityType": "Group",
        "entityId": 5,
        "newValue": { "code": "NEW-TEAM", "name": "新團隊" },
        "ipAddress": "192.168.1.100",
        "createdAt": "2025-12-31T10:00:00Z"
      }
    ],
    "pagination": { ... }
  }
}
```
