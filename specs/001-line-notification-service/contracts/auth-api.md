# API Contract: Auth API

**版本**: v1  
**Base Path**: `/api/v1/auth`

## Line Login 認證

### GET /api/v1/auth/line/login

取得 Line Login 授權 URL，用於前端重導向。

**認證**: 無

**回應**:

```json
{
  "success": true,
  "data": {
    "authUrl": "https://access.line.me/oauth2/v2.1/authorize?..."
  }
}
```

---

### POST /api/v1/auth/line/callback

處理 Line Login OAuth 回調，完成登入或註冊。

**認證**: 無

**請求**:

```json
{
  "code": "授權碼",
  "state": "狀態碼"
}
```

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 604800,
    "user": {
      "id": 123,
      "displayName": "王小明",
      "avatarUrl": "https://profile.line-scdn.net/...",
      "isAdmin": false,
      "groups": [],
      "isNewUser": true
    }
  }
}
```

**錯誤回應**:

| HTTP Code | Error Code | 說明 |
|-----------|------------|------|
| 400 | INVALID_CODE | 授權碼無效 |
| 400 | INVALID_STATE | 狀態碼不匹配 |
| 403 | USER_DISABLED | 使用者帳號已停用 |

---

## 管理員登入

### POST /api/v1/auth/admin/login

管理員帳號密碼登入。

**認證**: 無

**請求**:

```json
{
  "username": "ADMIN",
  "password": "password123"
}
```

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 604800,
    "admin": {
      "id": 1,
      "username": "ADMIN",
      "displayName": "系統管理員",
      "isSuperAdmin": true,
      "mustChangePassword": true
    }
  }
}
```

**錯誤回應**:

| HTTP Code | Error Code | 說明 |
|-----------|------------|------|
| 400 | INVALID_CREDENTIALS | 帳號或密碼錯誤 |
| 403 | ADMIN_DISABLED | 管理員帳號已停用 |
| 429 | TOO_MANY_ATTEMPTS | 登入嘗試過多，請稍後再試 |

---

### POST /api/v1/auth/admin/change-password

管理員修改密碼。

**認證**: JWT Token（管理員）

**請求**:

```json
{
  "currentPassword": "舊密碼",
  "newPassword": "新密碼",
  "confirmPassword": "確認新密碼"
}
```

**密碼規則**:
- 最少 8 個字元
- 至少包含一個大寫字母
- 至少包含一個小寫字母
- 至少包含一個數字

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "message": "密碼已成功修改"
}
```

---

## Token 管理

### POST /api/v1/auth/refresh

刷新 JWT Token。

**認證**: 無（使用 Refresh Token）

**請求**:

```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIs..."
}
```

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 604800
  }
}
```

---

### POST /api/v1/auth/logout

登出並使 Token 失效。

**認證**: JWT Token

**成功回應** (HTTP 200):

```json
{
  "success": true,
  "message": "已成功登出"
}
```

---

## 取得當前使用者資訊

### GET /api/v1/auth/me

取得當前登入使用者的詳細資訊。

**認證**: JWT Token

**回應（一般使用者）**:

```json
{
  "success": true,
  "data": {
    "id": 123,
    "displayName": "王小明",
    "avatarUrl": "https://profile.line-scdn.net/...",
    "email": "user@example.com",
    "isAdmin": false,
    "groups": [
      {
        "id": 1,
        "code": "INFRA",
        "name": "基礎設施團隊"
      }
    ],
    "subscribedMessageTypes": [
      {
        "code": "CRITICAL",
        "name": "嚴重"
      },
      {
        "code": "WARNING",
        "name": "警告"
      }
    ],
    "createdAt": "2025-12-01T00:00:00Z",
    "lastLoginAt": "2025-12-31T10:00:00Z"
  }
}
```

**回應（管理員）**:

```json
{
  "success": true,
  "data": {
    "id": 1,
    "username": "ADMIN",
    "displayName": "系統管理員",
    "isSuperAdmin": true,
    "isAdmin": true,
    "linkedUser": {
      "id": 123,
      "displayName": "王小明"
    },
    "createdAt": "2025-01-01T00:00:00Z",
    "lastLoginAt": "2025-12-31T10:00:00Z"
  }
}
```
