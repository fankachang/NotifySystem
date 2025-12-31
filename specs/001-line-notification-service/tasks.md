# Tasks: Line 訊息通知服務

**Input**: Design documents from `/specs/001-line-notification-service/`
**Prerequisites**: plan.md ✓, spec.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: 未明確要求測試，本任務清單不包含測試任務。

**Organization**: 任務按用戶故事分組，每個故事可獨立實作和測試。

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: 可平行執行（不同檔案、無依賴）
- **[Story]**: 任務所屬的用戶故事（如 US1, US2, US3）
- 描述中包含確切的檔案路徑

## Path Conventions

依據 plan.md 專案結構：
- 後端專案：`src/LineNotify.Api/`
- 測試專案：`src/LineNotify.Tests/`
- 容器配置：`docker/`

---

## Phase 1: Setup（共用基礎設施）

**目的**: 專案初始化與基礎結構（Phase 0 已完成大部分）

- [ ] T001 驗證現有專案結構與 plan.md 一致性
- [ ] T002 [P] 建立 DTOs 資料夾結構 src/LineNotify.Api/DTOs/Requests/ 和 src/LineNotify.Api/DTOs/Responses/
- [ ] T003 [P] 建立 Services 介面與實作資料夾 src/LineNotify.Api/Services/
- [ ] T004 [P] 建立 BackgroundServices 資料夾 src/LineNotify.Api/BackgroundServices/
- [ ] T005 [P] 建立 Middleware 資料夾 src/LineNotify.Api/Middleware/
- [ ] T006 [P] 建立 Configuration 資料夾與 LineSettings.cs src/LineNotify.Api/Configuration/LineSettings.cs

---

## Phase 2: Foundational（阻塞性前置任務）

**目的**: 所有用戶故事都依賴的核心基礎設施

**⚠️ 關鍵**: 此階段必須完成後才能開始任何用戶故事

### 資料庫與實體基礎

- [ ] T007 補完 GroupMember 實體關聯 src/LineNotify.Api/Models/GroupMember.cs（加入 JoinedAt 欄位）
- [ ] T008 補完 GroupMessageType 實體關聯 src/LineNotify.Api/Models/GroupMessageType.cs
- [ ] T009 更新 AppDbContext 配置所有實體關聯與索引 src/LineNotify.Api/Data/AppDbContext.cs
- [ ] T010 產生 EF Core 資料庫遷移並套用 src/LineNotify.Api/Data/Migrations/

### 認證與授權基礎架構

- [ ] T011 建立 JWT Token 服務介面 src/LineNotify.Api/Services/IJwtService.cs
- [ ] T012 實作 JWT Token 服務（產生、驗證、刷新）src/LineNotify.Api/Services/JwtService.cs
- [ ] T014 建立統一 API 回應格式 DTO src/LineNotify.Api/DTOs/Responses/ApiResponse.cs
- [ ] T015 [P] 建立錯誤碼與例外處理類別 src/LineNotify.Api/Exceptions/ApiException.cs

### 中介軟體基礎

- [ ] T016 實作全域例外處理中介軟體 src/LineNotify.Api/Middleware/ExceptionHandlerMiddleware.cs
- [ ] T017 實作審計日誌中介軟體 src/LineNotify.Api/Middleware/AuditLogMiddleware.cs
- [ ] T018 在 Program.cs 註冊所有中介軟體與服務 src/LineNotify.Api/Program.cs

**Checkpoint**: 基礎架構就緒 - 用戶故事實作可以開始

---

## Phase 3: User Story 1 - 快速註冊並等待管理員分配群組 (Priority: P1) 🎯 MVP

**目標**: 使用者可透過 Line Login 完成註冊，等待管理員分配群組後接收告警

**獨立測試**: 使用者點擊「使用 Line 登入」→ 完成 Line 授權 → 系統建立帳號 → 顯示「等待管理員分配群組」頁面

### DTOs for User Story 1

- [ ] T019 [P] [US1] 建立 Line Login 請求/回應 DTO src/LineNotify.Api/DTOs/Requests/LineLoginRequest.cs
- [ ] T020 [P] [US1] 建立 Token 回應 DTO src/LineNotify.Api/DTOs/Responses/TokenResponse.cs
- [ ] T021 [P] [US1] 建立 User 回應 DTO src/LineNotify.Api/DTOs/Responses/UserResponse.cs

### Services for User Story 1

- [ ] T022 [US1] 建立 Line Auth 服務介面 src/LineNotify.Api/Services/ILineAuthService.cs
- [ ] T023 [US1] 實作 Line Auth 服務（OAuth 流程）src/LineNotify.Api/Services/LineAuthService.cs
- [ ] T024 [US1] 建立登入記錄服務 src/LineNotify.Api/Services/ILoginLogService.cs
- [ ] T025 [US1] 實作登入記錄服務 src/LineNotify.Api/Services/LoginLogService.cs

### Controllers for User Story 1

- [ ] T026 [US1] 實作 AuthController - Line Login 端點 src/LineNotify.Api/Controllers/AuthController.cs
  - GET /api/v1/auth/line/login
  - POST /api/v1/auth/line/callback
  - POST /api/v1/auth/refresh
  - POST /api/v1/auth/logout
  - GET /api/v1/auth/me
  - PATCH /api/v1/auth/me（使用者更新顯示名稱）

### Frontend for User Story 1

- [ ] T027 [US1] 建立 Line Login 入口頁面 src/LineNotify.Api/Pages/Login.cshtml
- [ ] T028 [US1] 建立「等待管理員分配群組」提示頁面 src/LineNotify.Api/Pages/WaitingForGroup.cshtml
- [ ] T029 [US1] 建立使用者儀表板頁面（查看群組與訂閱、發送測試訊息）src/LineNotify.Api/Pages/Dashboard.cshtml

**Checkpoint**: User Story 1 完成 - 使用者可以透過 Line Login 註冊並查看自己的狀態

---

## Phase 4: User Story 2 - 群組化管理告警接收者 (Priority: P1)

**目標**: 管理員可建立群組、設定訊息類型、將使用者加入群組，自動同步訂閱

**獨立測試**: 管理員建立群組 → 設定訊息類型 → 加入使用者 → 驗證訂閱自動建立

### DTOs for User Story 2

- [ ] T030 [P] [US2] 建立 Group 請求 DTO src/LineNotify.Api/DTOs/Requests/GroupRequest.cs
- [ ] T031 [P] [US2] 建立 Group 回應 DTO src/LineNotify.Api/DTOs/Responses/GroupResponse.cs
- [ ] T032 [P] [US2] 建立 GroupMembers 請求 DTO src/LineNotify.Api/DTOs/Requests/GroupMembersRequest.cs
- [ ] T033 [P] [US2] 建立 MessageType 請求/回應 DTO src/LineNotify.Api/DTOs/Requests/MessageTypeRequest.cs

### Services for User Story 2

- [ ] T034 [US2] 建立群組服務介面 src/LineNotify.Api/Services/IGroupService.cs
- [ ] T035 [US2] 實作群組服務（CRUD、成員管理、時段設定衝突驗證）src/LineNotify.Api/Services/GroupService.cs
- [ ] T036 [US2] 建立訊息類型服務介面 src/LineNotify.Api/Services/IMessageTypeService.cs
- [ ] T037 [US2] 實作訊息類型服務 src/LineNotify.Api/Services/MessageTypeService.cs
- [ ] T038 [US2] 建立訂閱同步服務介面 src/LineNotify.Api/Services/ISubscriptionService.cs
- [ ] T039 [US2] 實作訂閱同步服務（自動同步邏輯）src/LineNotify.Api/Services/SubscriptionService.cs

### Controllers for User Story 2

- [ ] T040 [US2] 實作 GroupsController src/LineNotify.Api/Controllers/GroupsController.cs
  - GET/POST /api/v1/admin/groups
  - GET/PUT/DELETE /api/v1/admin/groups/{id}
  - POST/DELETE /api/v1/admin/groups/{id}/members
- [ ] T041 [US2] 實作 MessageTypesController src/LineNotify.Api/Controllers/MessageTypesController.cs
  - GET/POST/PUT/DELETE /api/v1/admin/message-types

### 管理員認證 for User Story 2

- [ ] T042 [US2] 建立管理員登入請求 DTO src/LineNotify.Api/DTOs/Requests/AdminLoginRequest.cs
- [ ] T043 [US2] 建立管理員服務介面與實作 src/LineNotify.Api/Services/IAdminService.cs
- [ ] T044 [US2] 實作管理員服務（登入、密碼修改、超級管理員權限檢查）src/LineNotify.Api/Services/AdminService.cs
- [ ] T045 [US2] 實作 AuthController - 管理員登入端點 src/LineNotify.Api/Controllers/AuthController.cs
  - POST /api/v1/auth/admin/login
  - POST /api/v1/auth/admin/change-password

**Checkpoint**: User Story 2 完成 - 管理員可以管理群組與使用者，訂閱自動同步

---

## Phase 5: User Story 3 - 外部系統整合（API 發送）(Priority: P1)

**目標**: 外部系統可透過 API 發送訊息，系統根據訂閱發送給 Line 使用者

**獨立測試**: 使用 curl 呼叫 API → 傳遞告警訊息 → 驗證訂閱者收到 Line 訊息

### DTOs for User Story 3

- [ ] T046 [P] [US3] 建立訊息發送請求 DTO src/LineNotify.Api/DTOs/Requests/SendMessageRequest.cs
- [ ] T047 [P] [US3] 建立訊息發送回應 DTO src/LineNotify.Api/DTOs/Responses/SendMessageResponse.cs
- [ ] T048 [P] [US3] 建立訊息查詢回應 DTO src/LineNotify.Api/DTOs/Responses/MessageResponse.cs

### API Key 認證 for User Story 3

- [ ] T049 [US3] 實作 API Key 認證中介軟體 src/LineNotify.Api/Middleware/ApiKeyAuthMiddleware.cs
- [ ] T050 [US3] 在 Program.cs 註冊 API Key 認證 src/LineNotify.Api/Program.cs

### Services for User Story 3

- [ ] T051 [US3] 建立 Line Messaging 服務介面 src/LineNotify.Api/Services/ILineMessagingService.cs
- [ ] T052 [US3] 實作 Line Messaging 服務（Flex Message）src/LineNotify.Api/Services/LineMessagingService.cs
- [ ] T053 [US3] 建立訊息派送服務介面 src/LineNotify.Api/Services/IMessageDispatchService.cs
- [ ] T054 [US3] 實作訊息派送服務（查詢訂閱者、去重）src/LineNotify.Api/Services/MessageDispatchService.cs

### Background Services for User Story 3

- [ ] T055 [US3] 實作非同步訊息發送背景服務 src/LineNotify.Api/BackgroundServices/MessageSenderService.cs
- [ ] T056 [US3] 實作失敗重試背景服務 src/LineNotify.Api/BackgroundServices/RetryService.cs

### Controllers for User Story 3

- [ ] T057 [US3] 實作 MessagesController src/LineNotify.Api/Controllers/MessagesController.cs
  - POST /api/v1/messages/send
  - GET /api/v1/messages/{id}
  - GET /api/v1/messages
  - GET /api/v1/messages/me

**Checkpoint**: User Story 3 完成 - 外部系統可發送訊息，使用者收到 Line 通知

---

## Phase 6: User Story 4 - 後台管理與監控 (Priority: P2)

**目標**: 管理員可透過 Web 後台管理使用者、查看歷史、產生報表

**獨立測試**: 管理員登入後台 → 查看使用者列表 → 查看發送統計報表

### DTOs for User Story 4

- [ ] T058 [P] [US4] 建立使用者查詢回應 DTO src/LineNotify.Api/DTOs/Responses/UserListResponse.cs
- [ ] T059 [P] [US4] 建立報表回應 DTO src/LineNotify.Api/DTOs/Responses/ReportResponse.cs
- [ ] T060 [P] [US4] 建立審計日誌回應 DTO src/LineNotify.Api/DTOs/Responses/AuditLogResponse.cs

### Services for User Story 4

- [ ] T061 [US4] 建立使用者管理服務 src/LineNotify.Api/Services/IUserService.cs
- [ ] T062 [US4] 實作使用者管理服務 src/LineNotify.Api/Services/UserService.cs
- [ ] T063 [US4] 建立報表服務介面 src/LineNotify.Api/Services/IReportService.cs
- [ ] T064 [US4] 實作報表服務（統計、趨勢分析）src/LineNotify.Api/Services/ReportService.cs

### Controllers for User Story 4

- [ ] T065 [US4] 實作 UsersController src/LineNotify.Api/Controllers/UsersController.cs
  - GET /api/v1/admin/users
  - PATCH /api/v1/admin/users/{id}
- [ ] T066 [US4] 實作 ReportsController src/LineNotify.Api/Controllers/ReportsController.cs
  - GET /api/v1/admin/reports/summary
  - GET /api/v1/admin/reports/delivery-stats
- [ ] T067 [US4] 實作審計日誌端點 src/LineNotify.Api/Controllers/AuditLogsController.cs
  - GET /api/v1/admin/audit-logs

### 管理後台 UI for User Story 4

- [ ] T068 [US4] 建立管理後台佈局 src/LineNotify.Api/Pages/Admin/_Layout.cshtml
- [ ] T069 [US4] 建立管理員登入頁面 src/LineNotify.Api/Pages/Admin/Login.cshtml
- [ ] T070 [US4] 建立使用者管理頁面 src/LineNotify.Api/Pages/Admin/Users.cshtml
- [ ] T071 [US4] 建立群組管理頁面 src/LineNotify.Api/Pages/Admin/Groups.cshtml
- [ ] T072 [US4] 建立訊息類型管理頁面 src/LineNotify.Api/Pages/Admin/MessageTypes.cshtml
- [ ] T073 [US4] 建立訊息歷史頁面 src/LineNotify.Api/Pages/Admin/Messages.cshtml
- [ ] T074 [US4] 建立統計報表頁面 src/LineNotify.Api/Pages/Admin/Reports.cshtml

**Checkpoint**: User Story 4 完成 - 管理員可透過後台完整管理系統

---

## Phase 7: User Story 5 - API Key 管理 (Priority: P2)

**目標**: 管理員可建立、管理、撤銷 API Key

**獨立測試**: 管理員建立 API Key → 複製使用 → 撤銷 Key → 驗證後續請求被拒絕

### DTOs for User Story 5

- [ ] T075 [P] [US5] 建立 API Key 請求 DTO src/LineNotify.Api/DTOs/Requests/ApiKeyRequest.cs
- [ ] T076 [P] [US5] 建立 API Key 回應 DTO src/LineNotify.Api/DTOs/Responses/ApiKeyResponse.cs

### Services for User Story 5

- [ ] T077 [US5] 建立 API Key 服務介面 src/LineNotify.Api/Services/IApiKeyService.cs
- [ ] T078 [US5] 實作 API Key 服務（建立、雜湊、驗證、撤銷）src/LineNotify.Api/Services/ApiKeyService.cs

### Controllers for User Story 5

- [ ] T079 [US5] 實作 ApiKeysController src/LineNotify.Api/Controllers/ApiKeysController.cs
  - GET /api/v1/admin/api-keys
  - POST /api/v1/admin/api-keys
  - DELETE /api/v1/admin/api-keys/{id}

### UI for User Story 5

- [ ] T080 [US5] 建立 API Key 管理頁面 src/LineNotify.Api/Pages/Admin/ApiKeys.cshtml

**Checkpoint**: User Story 5 完成 - 管理員可完整管理 API Key

---

## Phase 8: 進階功能（Edge Cases & Cross-Cutting）

**目的**: 處理 spec.md 中的 Edge Cases 和進階需求

### 群組篩選與時段控制

- [ ] T081 [P] 實作來源主機/服務萬用字元篩選邏輯 src/LineNotify.Api/Services/MessageDispatchService.cs
- [ ] T082 [P] 實作接收時段與靜音時段判斷邏輯 src/LineNotify.Api/Services/MessageDispatchService.cs
- [ ] T083 實作重複告警抑制機制 src/LineNotify.Api/Services/MessageDispatchService.cs

### Rate Limiting

- [ ] T084 實作 Rate Limiting 中介軟體 src/LineNotify.Api/Middleware/RateLimitingMiddleware.cs
- [ ] T085 在 Program.cs 配置 Rate Limiting 規則 src/LineNotify.Api/Program.cs

### 資料清理

- [ ] T086 實作 90 天資料自動清理背景服務 src/LineNotify.Api/BackgroundServices/DataCleanupService.cs

### Edge Cases 處理

- [ ] T087 實作訊息內容驗證與截斷邏輯 src/LineNotify.Api/Services/LineMessagingService.cs
- [ ] T088 實作 Line 綁定失效檢測與處理 src/LineNotify.Api/Services/LineMessagingService.cs

---

## Phase 9: Polish & 部署準備

**目的**: 完善文件與部署配置

- [ ] T089 [P] 更新 appsettings.json 範例配置 src/LineNotify.Api/appsettings.json
- [ ] T090 [P] 建立 appsettings.Development.example.json src/LineNotify.Api/appsettings.Development.example.json
- [ ] T091 [P] 完善 Dockerfile 多階段建置 docker/Dockerfile
- [ ] T092 [P] 建立生產環境 docker-compose.prod.yml docker/docker-compose.prod.yml
- [ ] T093 [P] 撰寫 API 參考文件 docs/api-reference.md
- [ ] T094 [P] 撰寫部署指南 docs/deployment.md
- [ ] T095 [P] 撰寫 Nagios 整合指南 docs/nagios-integration.md
- [ ] T096 執行 quickstart.md 驗證完整流程

### 資料庫備份

- [ ] T097 [P] 配置 MySQL 自動備份腳本與排程 docker/mysql/backup.sh
- [ ] T098 [P] 在 docker-compose.prod.yml 設定備份 Volume 掛載與 cron 排程

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: 無依賴 - 可立即開始
- **Phase 2 (Foundational)**: 依賴 Phase 1 完成 - **阻塞所有用戶故事**
- **Phase 3-7 (User Stories)**: 依賴 Phase 2 完成
  - US1, US2, US3 為 P1 優先級，應優先完成
  - US4, US5 為 P2 優先級，可在 P1 完成後開始
- **Phase 8 (進階功能)**: 依賴 US3 完成（訊息派送服務）
- **Phase 9 (Polish)**: 依賴所有功能完成

### User Story Dependencies

```
Phase 2 (Foundational)
        │
        ▼
   ┌────┴────┐
   ▼         ▼
  US1       US2 ◄───┐
   │         │      │
   └────┬────┘      │（管理員認證共用）
        │           │
        ▼           │
       US3 ─────────┘
        │
        ▼
   ┌────┴────┐
   ▼         ▼
  US4       US5
```

- **US1 (Line Login)**: Phase 2 完成後可開始，無其他依賴
- **US2 (群組管理)**: Phase 2 完成後可開始，無其他依賴
- **US3 (訊息發送)**: 最好在 US1、US2 之後（需要使用者和群組），但可平行開發
- **US4 (後台管理)**: 依賴 US1、US2 的基礎功能
- **US5 (API Key)**: 可與 US4 平行開發

### 平行機會

- Phase 1 所有 [P] 任務可平行
- Phase 2 所有 [P] 任務可平行
- 每個 User Story 內的 DTOs [P] 任務可平行
- US1 與 US2 可由不同開發者平行開發
- US4 與 US5 可平行開發

---

## Parallel Example: User Story 1

```bash
# 平行建立所有 DTOs：
Task: T019 建立 Line Login 請求/回應 DTO
Task: T020 建立 Token 回應 DTO
Task: T021 建立 User 回應 DTO

# 序列建立 Services（有依賴關係）：
Task: T022 → T023 (Line Auth Service)
Task: T024 → T025 (Login Log Service)
```

---

## Implementation Strategy

### MVP First (Phase 1-3 + US1-US3)

1. ✅ 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（關鍵阻塞點）
3. 完成 Phase 3: User Story 1 (Line Login)
4. 完成 Phase 4: User Story 2 (群組管理)
5. 完成 Phase 5: User Story 3 (訊息發送)
6. **STOP and VALIDATE**: 測試核心流程
7. 部署 MVP（可發送告警的最小可用產品）

### Incremental Delivery

1. Setup + Foundational → 基礎就緒
2. 加入 US1 → 使用者可登入註冊
3. 加入 US2 → 管理員可管理群組
4. 加入 US3 → 可發送訊息（**MVP 完成**）
5. 加入 US4 → 完整後台管理
6. 加入 US5 → API Key 管理
7. 加入 Phase 8 → 進階功能
8. Phase 9 → 部署準備

### 預估時程

| Phase | 任務數 | 預估工時 |
|-------|--------|----------|
| Phase 1: Setup | 6 | 2h |
| Phase 2: Foundational | 12 | 8h |
| Phase 3: US1 | 11 | 8h |
| Phase 4: US2 | 16 | 12h |
| Phase 5: US3 | 12 | 12h |
| Phase 6: US4 | 17 | 16h |
| Phase 7: US5 | 6 | 4h |
| Phase 8: 進階功能 | 8 | 8h |
| Phase 9: Polish | 8 | 4h |
| **Total** | **96** | **~74h** |

---

## Notes

- [P] 任務 = 不同檔案、無依賴
- [Story] 標籤將任務對應到特定用戶故事以便追蹤
- 每個用戶故事應可獨立完成和測試
- 每個任務或邏輯群組完成後提交
- 可在任何 Checkpoint 停下來獨立驗證故事
- 避免：模糊任務、同檔案衝突、破壞獨立性的跨故事依賴
