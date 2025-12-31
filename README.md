# Line Notification Service (LNS)

整合 Nagios 監控告警與 LINE 推播通知的服務系統，讓運維人員能即時透過 LINE 接收系統告警。

## ✨ 功能特色

- 🔔 **即時告警通知** - 整合 Nagios 監控系統，第一時間推播告警
- 👥 **群組化管理** - 依團隊、服務分類，精準發送告警給對應人員
- 🔐 **LINE Login 整合** - 使用者透過 LINE 帳號登入，自動綁定通知
- 📊 **完整追蹤記錄** - 訊息發送歷史、統計報表，完整掌握告警狀況
- 🔑 **API Key 認證** - 支援外部系統透過 API Key 安全發送通知

## 🏗️ 技術架構

- **後端框架**: ASP.NET Core 10.0
- **資料庫**: MySQL 8.0
- **認證機制**: JWT + LINE Login OAuth 2.0
- **容器化**: Docker / Podman

## 📁 專案結構

```
NotifySystem/
├── src/
│   └── LineNotify.Api/          # 主要 API 專案
│       ├── Controllers/         # API 控制器
│       ├── Services/            # 業務邏輯服務
│       ├── Models/              # 資料模型
│       ├── Pages/               # Razor Pages (前端頁面)
│       └── Data/                # 資料庫相關
├── docker/                      # Docker 部署配置
├── docs/                        # 文件
│   ├── api-reference.md         # API 參考文件
│   ├── deployment.md            # 部署指南
│   └── nagios-integration.md    # Nagios 整合說明
└── specs/                       # 規格文件
```

## 🚀 快速開始

### 前置需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) 或 [Podman](https://podman.io/)
- [MySQL 8.0](https://www.mysql.com/) (或使用 Docker 容器)

### 本地開發

1. **複製專案**

```bash
git clone https://github.com/fankachang/NotifySystem.git
cd NotifySystem
```

2. **啟動 MySQL 容器**

```bash
cd docker
docker-compose up -d mysql
```

3. **配置環境**

```bash
cd src/LineNotify.Api
cp appsettings.Development.example.json appsettings.Development.json
# 編輯 appsettings.Development.json 填入必要配置
```

4. **執行資料庫遷移**

```bash
dotnet ef database update
```

5. **啟動服務**

```bash
dotnet run --urls "http://0.0.0.0:5050"
```

6. **存取服務**

- 前端頁面: http://localhost:5050
- 管理後台: http://localhost:5050/Admin
- API 文件: http://localhost:5050/swagger

### 測試 LINE Login (使用 ngrok)

由於 LINE OAuth 需要公開的 HTTPS 網址，本地開發時需使用 ngrok：

```bash
# 安裝 ngrok (macOS)
brew install ngrok

# 設定 authtoken (需先至 ngrok.com 註冊)
ngrok config add-authtoken YOUR_TOKEN

# 啟動通道
ngrok http 5050
```

詳細說明請參考 [部署指南](docs/deployment.md#本地開發---使用-ngrok-測試-line-login)。

### Docker 部署

```bash
cd docker
docker-compose -f docker-compose.prod.yml up -d --build
```

## 📖 文件

- [API 參考文件](docs/api-reference.md) - 完整的 API 端點說明
- [部署指南](docs/deployment.md) - 生產環境部署與 ngrok 設定
- [Nagios 整合](docs/nagios-integration.md) - 與 Nagios 監控系統整合

## 🔧 API 端點概覽

### 認證 API
| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/auth/line/login` | 取得 LINE Login 授權 URL |
| GET | `/api/v1/auth/line/callback` | LINE OAuth 回調 |
| POST | `/api/v1/auth/admin/login` | 管理員登入 |
| GET | `/api/v1/auth/me` | 取得當前使用者資訊 |

### 訊息 API
| 方法 | 路徑 | 說明 |
|------|------|------|
| POST | `/api/v1/messages` | 發送通知訊息 |
| GET | `/api/v1/messages` | 查詢訊息歷史 |

### 管理 API
| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/admin/users` | 使用者列表 |
| GET | `/api/v1/admin/groups` | 群組列表 |
| GET | `/api/v1/admin/api-keys` | API Key 列表 |

## 🔑 預設帳號

首次部署後，使用以下預設管理員帳號登入：

- **帳號**: `ADMIN`
- **密碼**: `Admin@2025!`

> ⚠️ **重要**: 首次登入後請立即修改密碼！

## 📄 授權

本專案採用 [MIT License](https://opensource.org/licenses/MIT) - 詳見 [LICENSE](LICENSE) 檔案。
