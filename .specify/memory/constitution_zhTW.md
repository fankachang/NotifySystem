# 專案架構約束: Line 訊息通知服務

> 本文件定義專案生命週期中必須遵循的架構約束和技術決策。

## 核心原則

### 1. 簡單優先

- **單一後端專案**：使用一個 ASP.NET Core Web API 專案。不採用微服務架構。
- **單一資料庫**：僅使用 MySQL。不採用多型態持久化。
- **不使用訊息佇列**：使用 .NET BackgroundService 處理非同步作業。不使用 RabbitMQ/Kafka。
- **前端整合**：Razor Pages 整合於同一專案。不使用獨立的 SPA 框架。

### 2. 技術堆疊（已鎖定）

| 層級 | 技術 | 版本 | 理由 |
|------|------|------|------|
| 執行環境 | .NET | 10.x | 最新 LTS 路徑，C# 語言特性 |
| Web 框架 | ASP.NET Core | 10.x | .NET 10 內建 |
| ORM | Entity Framework Core | 10.x | Code First，MySQL provider |
| 資料庫 | MySQL | 8.x | 使用者需求，Docker 容器化 |
| 認證 | JWT Bearer | - | 無狀態 API 認證 |
| 密碼雜湊 | BCrypt | - | 業界標準 |
| 容器 | Docker/Podman | - | 容器化部署 |

### 3. 禁止模式

以下模式**明確禁止**使用，除非在 `Complexity Tracking` 中提供充分理由：

- [ ] 微服務架構
- [ ] 多資料庫或多種資料庫類型
- [ ] 訊息佇列（RabbitMQ、Kafka、Azure Service Bus）
- [ ] 獨立前端部署（React、Vue、Angular 作為獨立應用）
- [ ] EF Core 上的 Repository 抽象層
- [ ] CQRS/Event Sourcing
- [ ] GraphQL（僅使用 REST）
- [ ] gRPC 內部通訊

### 4. 必要模式

以下模式**必須**使用：

- [x] 依賴注入（.NET 內建 DI）
- [x] 透過 appsettings.json + 環境變數進行配置
- [x] 結構化日誌（Microsoft.Extensions.Logging）
- [x] 健康檢查端點（`/health`）
- [x] OpenAPI 文件（.NET 10 內建）
- [x] Entity Framework Core 遷移（Code First）

## 專案邊界

### 規模限制

| 指標 | 目標 | 硬性限制 |
|------|------|---------|
| 每日訊息量 | 10,000+ | 100,000 |
| 並發使用者 | 100 | 500 |
| API 回應時間 (p95) | <500ms | <2s |
| 端到端延遲 | <3s | <10s |
| 資料保留期限 | 90 天 | - |

### 外部依賴

| 服務 | 用途 | 重要性 |
|------|------|--------|
| Line Login API | OAuth 認證 | 高 |
| Line Messaging API | 訊息發送 | 關鍵 |
| MySQL | 資料持久化 | 關鍵 |

## 變更控制

### 新增技術

在新增任何未列於上述的新技術或模式之前：

1. 記錄其解決的具體問題
2. 解釋為何現有模式不足以應對
3. 在 plan.md 的 `Complexity Tracking` 新增項目
4. 取得明確批准

### 例外情況

本約束的例外需要：

1. 在 `Complexity Tracking` 中提供明確理由
2. 證明已評估過更簡單的替代方案
3. 記錄接受的取捨

---

*最後更新：2025-12-31*
*版本：1.0*
