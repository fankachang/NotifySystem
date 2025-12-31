# Data Model: Line 訊息通知服務

**版本**: 1.0  
**更新日期**: 2025-12-31

## Entity Relationship Diagram (概念)

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│    Admin    │       │    User     │       │  LoginLog   │
│─────────────│       │─────────────│       │─────────────│
│ Id          │       │ Id          │◄──────│ UserId      │
│ Username    │       │ LineUserId  │       │ IpAddress   │
│ PasswordHash│       │ DisplayName │       │ UserAgent   │
│ IsSuperAdmin│       │ AvatarUrl   │       │ LoginTime   │
│ ...         │       │ IsActive    │       │ IsSuccess   │
└─────────────┘       │ CreatedAt   │       └─────────────┘
                      │ LastLoginAt │
                      └──────┬──────┘
                             │
                             │ M:N
                             ▼
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│ MessageType │◄──────│    Group    │──────►│ UserGroup   │
│─────────────│  M:N  │─────────────│       │(Join Table) │
│ Id          │       │ Id          │       │─────────────│
│ Code        │       │ Code        │       │ UserId      │
│ Name        │       │ Name        │       │ GroupId     │
│ Priority    │       │ HostFilter  │       └─────────────┘
│ Color       │       │ ServiceFilter│
│ IsDefault   │       │ ActiveStart │
│ IsActive    │       │ ActiveEnd   │
└──────┬──────┘       │ MuteStart   │
       │              │ MuteEnd     │
       │              │ SuppressDup │
       │              │ DupInterval │
       ▼              │ IsActive    │
┌─────────────┐       └─────────────┘
│ Subscription│              │
│─────────────│              │ M:N
│ Id          │              ▼
│ UserId      │       ┌─────────────┐
│ GroupId     │       │GroupMsgType │
│ MsgTypeId   │       │(Join Table) │
│ IsActive    │       │─────────────│
└─────────────┘       │ GroupId     │
                      │ MsgTypeId   │
                      └─────────────┘

┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│   Message   │──────►│MsgDelivery  │       │   ApiKey    │
│─────────────│  1:N  │─────────────│       │─────────────│
│ Id          │       │ Id          │       │ Id          │
│ MsgTypeId   │       │ MessageId   │       │ Name        │
│ Title       │       │ UserId      │       │ KeyHash     │
│ Content     │       │ Status      │       │ KeyPrefix   │
│ SourceHost  │       │ LineMessageId│      │ CreatedById │
│ SourceService│      │ SentAt      │       │ IsActive    │
│ SourceIp    │       │ ErrorMessage│       │ LastUsedAt  │
│ Metadata    │       │ RetryCount  │       │ ExpiresAt   │
│ Priority    │       └─────────────┘       └─────────────┘
│ CreatedAt   │
└─────────────┘
```

## Entity Definitions

### User（使用者）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| LineUserId | VARCHAR(50) | UNIQUE, NOT NULL | Line User ID |
| DisplayName | VARCHAR(100) | NOT NULL | 顯示名稱 |
| AvatarUrl | VARCHAR(500) | NULL | 頭像 URL |
| Email | VARCHAR(200) | NULL | Email（可選） |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 帳號狀態 |
| IsAdmin | BOOLEAN | NOT NULL, DEFAULT FALSE | 是否為管理員 |
| LineBindingValid | BOOLEAN | NOT NULL, DEFAULT TRUE | Line 綁定是否有效 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |
| LastLoginAt | DATETIME | NULL | 最後登入時間 |

**索引**:
- `IX_User_LineUserId` (LineUserId) UNIQUE

### Admin（管理員）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| Username | VARCHAR(50) | UNIQUE, NOT NULL | 帳號名稱 |
| PasswordHash | VARCHAR(200) | NOT NULL | 密碼雜湊（BCrypt） |
| DisplayName | VARCHAR(100) | NOT NULL | 顯示名稱 |
| IsSuperAdmin | BOOLEAN | NOT NULL, DEFAULT FALSE | 是否為超級管理員 |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 帳號狀態 |
| MustChangePassword | BOOLEAN | NOT NULL, DEFAULT FALSE | 是否需強制修改密碼 |
| LinkedUserId | BIGINT | FK(User), NULL | 關聯的 Line 使用者 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |
| LastLoginAt | DATETIME | NULL | 最後登入時間 |

**索引**:
- `IX_Admin_Username` (Username) UNIQUE

### Group（群組）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| Code | VARCHAR(50) | UNIQUE, NOT NULL | 群組代碼 |
| Name | VARCHAR(100) | NOT NULL | 群組名稱 |
| Description | VARCHAR(500) | NULL | 說明 |
| HostFilter | VARCHAR(200) | NULL | 來源主機篩選（萬用字元） |
| ServiceFilter | VARCHAR(200) | NULL | 來源服務篩選（萬用字元） |
| ActiveTimeStart | TIME | NULL, DEFAULT '00:00:00' | 接收時段開始 |
| ActiveTimeEnd | TIME | NULL, DEFAULT '23:59:59' | 接收時段結束 |
| MuteTimeStart | TIME | NULL | 靜音時段開始 |
| MuteTimeEnd | TIME | NULL | 靜音時段結束 |
| SuppressDuplicate | BOOLEAN | NOT NULL, DEFAULT TRUE | 是否抑制重複告警 |
| DuplicateIntervalMinutes | INT | NOT NULL, DEFAULT 30 | 重複告警間隔（分鐘） |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 啟用狀態 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |
| UpdatedAt | DATETIME | NOT NULL | 更新時間 |

**索引**:
- `IX_Group_Code` (Code) UNIQUE
- `IX_Group_IsActive` (IsActive)

### MessageType（訊息類型）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| Code | VARCHAR(50) | UNIQUE, NOT NULL | 類型代碼 |
| Name | VARCHAR(100) | NOT NULL | 類型名稱 |
| Description | VARCHAR(500) | NULL | 說明 |
| Priority | INT | NOT NULL, DEFAULT 3 | 優先級（1-5） |
| Color | VARCHAR(7) | NOT NULL, DEFAULT '#808080' | 顏色（十六進位） |
| Icon | VARCHAR(50) | NULL | 圖示名稱 |
| IsDefault | BOOLEAN | NOT NULL, DEFAULT FALSE | 是否為系統預設 |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 啟用狀態 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |

**索引**:
- `IX_MessageType_Code` (Code) UNIQUE
- `IX_MessageType_Priority` (Priority)

**預設資料**:
| Code | Name | Priority | Color | IsDefault |
|------|------|----------|-------|-----------|
| CRITICAL | 嚴重 | 1 | #FF0000 | TRUE |
| WARNING | 警告 | 2 | #FFA500 | TRUE |
| UNKNOWN | 未知 | 3 | #808080 | TRUE |
| OK | 正常 | 4 | #00FF00 | TRUE |
| INFO | 資訊 | 5 | #0000FF | TRUE |

### UserGroup（使用者群組關聯）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| UserId | BIGINT | PK, FK(User) | 使用者 ID |
| GroupId | BIGINT | PK, FK(Group) | 群組 ID |
| JoinedAt | DATETIME | NOT NULL | 加入時間 |

**索引**:
- `PK_UserGroup` (UserId, GroupId)

### GroupMessageType（群組訊息類型關聯）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| GroupId | BIGINT | PK, FK(Group) | 群組 ID |
| MessageTypeId | BIGINT | PK, FK(MessageType) | 訊息類型 ID |

**索引**:
- `PK_GroupMessageType` (GroupId, MessageTypeId)

### Subscription（訂閱）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| UserId | BIGINT | FK(User), NOT NULL | 使用者 ID |
| GroupId | BIGINT | FK(Group), NOT NULL | 群組 ID |
| MessageTypeId | BIGINT | FK(MessageType), NOT NULL | 訊息類型 ID |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 啟用狀態 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |

**索引**:
- `IX_Subscription_User_Group_MsgType` (UserId, GroupId, MessageTypeId) UNIQUE
- `IX_Subscription_MsgType_Active` (MessageTypeId, IsActive)

### Message（訊息）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| MessageTypeId | BIGINT | FK(MessageType), NOT NULL | 訊息類型 ID |
| Title | VARCHAR(200) | NOT NULL | 標題 |
| Content | TEXT | NOT NULL | 內容 |
| SourceHost | VARCHAR(100) | NULL | 來源主機 |
| SourceService | VARCHAR(100) | NULL | 來源服務 |
| SourceIp | VARCHAR(45) | NULL | 來源 IP |
| Metadata | JSON | NULL | 擴展資料 |
| Priority | ENUM('high','normal','low') | NOT NULL, DEFAULT 'normal' | 優先級 |
| ApiKeyId | BIGINT | FK(ApiKey), NULL | 發送者 API Key |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |

**索引**:
- `IX_Message_CreatedAt` (CreatedAt)
- `IX_Message_MsgType_CreatedAt` (MessageTypeId, CreatedAt)
- `IX_Message_SourceHost_Service` (SourceHost, SourceService)

### MessageDelivery（訊息發送記錄）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| MessageId | BIGINT | FK(Message), NOT NULL | 訊息 ID |
| UserId | BIGINT | FK(User), NOT NULL | 使用者 ID |
| Status | ENUM('pending','sent','failed','skipped') | NOT NULL, DEFAULT 'pending' | 發送狀態 |
| LineMessageId | VARCHAR(100) | NULL | Line 訊息 ID |
| SentAt | DATETIME | NULL | 發送時間 |
| ErrorMessage | VARCHAR(500) | NULL | 錯誤訊息 |
| RetryCount | INT | NOT NULL, DEFAULT 0 | 重試次數 |
| NextRetryAt | DATETIME | NULL | 下次重試時間 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |

**索引**:
- `IX_MsgDelivery_Message` (MessageId)
- `IX_MsgDelivery_User_CreatedAt` (UserId, CreatedAt)
- `IX_MsgDelivery_Status_NextRetry` (Status, NextRetryAt)

### ApiKey（API 金鑰）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| Name | VARCHAR(100) | NOT NULL | 金鑰名稱 |
| KeyHash | VARCHAR(64) | UNIQUE, NOT NULL | 金鑰雜湊（SHA-256） |
| KeyPrefix | VARCHAR(10) | NOT NULL | 金鑰前綴（用於顯示） |
| CreatedById | BIGINT | FK(Admin), NOT NULL | 建立者 |
| Permissions | JSON | NULL | 權限設定 |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | 啟用狀態 |
| ExpiresAt | DATETIME | NULL | 過期時間 |
| LastUsedAt | DATETIME | NULL | 最後使用時間 |
| CreatedAt | DATETIME | NOT NULL | 建立時間 |

**索引**:
- `IX_ApiKey_KeyHash` (KeyHash) UNIQUE
- `IX_ApiKey_IsActive` (IsActive)

### LoginLog（登入記錄）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| UserId | BIGINT | FK(User), NULL | 使用者 ID |
| AdminId | BIGINT | FK(Admin), NULL | 管理員 ID |
| IpAddress | VARCHAR(45) | NOT NULL | IP 位址 |
| UserAgent | VARCHAR(500) | NULL | User Agent |
| LoginType | ENUM('line','admin') | NOT NULL | 登入類型 |
| IsSuccess | BOOLEAN | NOT NULL | 是否成功 |
| FailReason | VARCHAR(200) | NULL | 失敗原因 |
| CreatedAt | DATETIME | NOT NULL | 登入時間 |

**索引**:
- `IX_LoginLog_User_CreatedAt` (UserId, CreatedAt)
- `IX_LoginLog_Admin_CreatedAt` (AdminId, CreatedAt)
- `IX_LoginLog_CreatedAt` (CreatedAt)

### AuditLog（審計日誌）

| 欄位 | 類型 | 約束 | 說明 |
|------|------|------|------|
| Id | BIGINT | PK, AUTO_INCREMENT | 主鍵 |
| AdminId | BIGINT | FK(Admin), NULL | 操作者（管理員） |
| Action | VARCHAR(50) | NOT NULL | 操作類型 |
| EntityType | VARCHAR(50) | NOT NULL | 實體類型 |
| EntityId | BIGINT | NULL | 實體 ID |
| OldValue | JSON | NULL | 舊值 |
| NewValue | JSON | NULL | 新值 |
| IpAddress | VARCHAR(45) | NOT NULL | IP 位址 |
| CreatedAt | DATETIME | NOT NULL | 操作時間 |

**索引**:
- `IX_AuditLog_Admin_CreatedAt` (AdminId, CreatedAt)
- `IX_AuditLog_EntityType_EntityId` (EntityType, EntityId)
- `IX_AuditLog_CreatedAt` (CreatedAt)

## Data Retention Policy

- **Message** 和 **MessageDelivery**: 保留 90 天，超過後由 DataCleanupService 自動刪除
- **LoginLog**: 保留 90 天
- **AuditLog**: 保留 365 天
- 其他實體資料永久保留（除非手動刪除）
