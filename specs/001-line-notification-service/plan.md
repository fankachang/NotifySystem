# Implementation Plan: Line 訊息通知服務

**Branch**: `001-line-notification-service` | **Date**: 2025-12-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-line-notification-service/spec.md`

## Summary

建立一個以 Line 為主要發送管道的訊息通知服務，用於替代傳統 Nagios 簡訊發送服務。系統採用 .NET 10 + ASP.NET Core Web API 架構，搭配 MySQL 資料庫，透過 Docker Compose 進行容器化部署。核心功能包括：Line Login OAuth 認證、群組化告警管理、RESTful API 供外部系統整合、以及管理後台介面。

## Technical Context

**Language/Version**: .NET 10（C#）  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, Line SDK  
**Storage**: MySQL 8.x（Docker 容器化，使用 Volume 持久化）  
**Testing**: xUnit, FluentAssertions, Moq  
**Target Platform**: Docker / Docker Compose（Linux 容器）  
**Project Type**: Web 應用程式（前後端分離，前端整合至同一專案）  
**Performance Goals**: API 回應 <500ms (p95), 端到端延遲 <3s, 支援 100 並發請求  
**Constraints**: 每日 10,000+ 訊息發送量, 99.5% 可用性  
**Scale/Scope**: 企業內部使用，預估數百位使用者

## Constitution Check

*GATE: 技術架構已確認，符合簡單架構原則*

- ✅ 單一後端專案（不使用微服務）
- ✅ 單一資料庫（MySQL）
- ✅ 不使用訊息佇列（改用 BackgroundService）
- ✅ 前後端整合（不分離部署）

## Project Structure

### Documentation (this feature)

```text
specs/001-line-notification-service/
├── plan.md              # 本檔案（實作計畫）
├── spec.md              # 功能規格
├── data-model.md        # 資料模型設計
├── quickstart.md        # 快速開始指南
├── contracts/           # API 合約定義
│   ├── messages-api.md
│   ├── auth-api.md
│   └── admin-api.md
└── tasks.md             # 任務分解（由 /speckit.tasks 產生）
```

### Source Code (repository root)

```text
src/
├── LineNotify.Api/                    # ASP.NET Core Web API 專案
│   ├── Controllers/                   # API 控制器
│   │   ├── AuthController.cs          # Line Login / JWT 認證
│   │   ├── MessagesController.cs      # 訊息發送 API
│   │   ├── UsersController.cs         # 使用者管理
│   │   ├── GroupsController.cs        # 群組管理
│   │   ├── MessageTypesController.cs  # 訊息類型管理
│   │   ├── ApiKeysController.cs       # API Key 管理
│   │   └── ReportsController.cs       # 報表與統計
│   ├── Models/                        # 資料模型（Entity）
│   │   ├── User.cs
│   │   ├── Admin.cs
│   │   ├── Group.cs
│   │   ├── MessageType.cs
│   │   ├── Subscription.cs
│   │   ├── Message.cs
│   │   ├── MessageDelivery.cs
│   │   ├── ApiKey.cs
│   │   └── LoginLog.cs
│   ├── DTOs/                          # 資料傳輸物件
│   │   ├── Requests/
│   │   └── Responses/
│   ├── Services/                      # 業務邏輯服務
│   │   ├── ILineAuthService.cs
│   │   ├── LineAuthService.cs
│   │   ├── ILineMessagingService.cs
│   │   ├── LineMessagingService.cs
│   │   ├── IMessageDispatchService.cs
│   │   ├── MessageDispatchService.cs
│   │   ├── ISubscriptionService.cs
│   │   └── SubscriptionService.cs
│   ├── BackgroundServices/            # 背景服務
│   │   ├── MessageSenderService.cs    # 非同步訊息發送
│   │   ├── RetryService.cs            # 失敗重試
│   │   └── DataCleanupService.cs      # 90天資料清理
│   ├── Data/                          # 資料存取層
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   ├── Middleware/                    # 中介軟體
│   │   ├── ApiKeyAuthMiddleware.cs
│   │   ├── RateLimitingMiddleware.cs
│   │   └── AuditLogMiddleware.cs
│   ├── Configuration/                 # 組態設定
│   │   └── LineSettings.cs
│   ├── wwwroot/                       # 靜態檔案（前端）
│   ├── Pages/                         # Razor Pages（前端整合）
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
│
└── LineNotify.Tests/                  # 測試專案
    ├── Unit/
    │   ├── Services/
    │   └── Controllers/
    ├── Integration/
    │   └── Api/
    └── Contract/
        └── ApiContracts/

docker/
├── docker-compose.yml                 # 開發環境編排
├── docker-compose.prod.yml            # 生產環境編排
├── Dockerfile                         # API 容器映像
└── mysql/
    └── init.sql                       # 資料庫初始化腳本

docs/
├── api-reference.md                   # API 參考文件
├── deployment.md                      # 部署指南
└── nagios-integration.md              # Nagios 整合指南
```

**Structure Decision**: 採用單一 ASP.NET Core 專案結構，前端使用 Razor Pages 整合至同一專案，避免額外的前端框架複雜度。使用 Entity Framework Core Code First 進行資料庫遷移。

## Implementation Phases

### Phase 0: 專案初始化與基礎架構

1. 建立 .NET 10 專案結構
2. 設定 Docker Compose（API + MySQL）
3. 設定 Entity Framework Core 與資料庫連線
4. 建立基礎實體模型與資料庫遷移
5. 設定 JWT 認證基礎架構

### Phase 1: 核心認證功能

1. 實作管理員帳號密碼登入
2. 實作 Line Login OAuth 2.0 整合
3. 實作 JWT Token 產生與刷新
4. 建立登入記錄功能

### Phase 2: 群組與訂閱管理

1. 實作群組 CRUD 操作
2. 實作訊息類型管理
3. 實作使用者加入/移出群組
4. 實作自動訂閱同步邏輯

### Phase 3: 訊息發送核心

1. 實作訊息發送 API
2. 實作 API Key 認證
3. 實作 Line Messaging API 整合
4. 實作非同步訊息發送（BackgroundService）
5. 實作重試機制

### Phase 4: 進階功能

1. 實作群組篩選（來源主機/服務、時段）
2. 實作重複告警抑制
3. 實作訊息去重
4. 實作 Rate Limiting

### Phase 5: 報表與管理介面

1. 實作訊息歷史查詢
2. 實作統計報表
3. 建立管理後台 UI
4. 建立使用者介面

### Phase 6: 部署與監控

1. 完善 Docker 配置
2. 設定資料庫備份
3. 設定監控與日誌
4. 撰寫部署文件

## Complexity Tracking

| 項目 | 決策 | 理由 |
|------|------|------|
| 不使用訊息佇列 | BackgroundService | 規模不大，內建方案足夠 |
| 前後端整合 | Razor Pages | 降低維護複雜度 |
| 單一資料庫 | MySQL | 符合使用者需求且足夠應付規模 |

## Dependencies & External Services

| 服務 | 用途 | 備註 |
|------|------|------|
| Line Login API | OAuth 認證 | 需申請 Line Login Channel |
| Line Messaging API | 發送訊息 | 需申請 Messaging API Channel |
| MySQL 8.x | 資料儲存 | Docker 容器化 |

## Risk Assessment

| 風險 | 影響 | 緩解措施 |
|------|------|---------|
| Line API 配額限制 | 高 | 監控使用量，必要時升級方案 |
| 資料庫容量成長 | 中 | 90天自動清理，定期監控 |
| 單點故障 | 中 | Docker Compose 重啟策略，備份機制 |
