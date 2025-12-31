# Quick Start Guide: Line 訊息通知服務

## 環境需求

- .NET 10 SDK
- Docker Desktop
- Git

## 快速開始

### 1. 複製專案

```bash
git clone <repository-url>
cd NotifySystem
```

### 2. 啟動開發環境

```bash
# 啟動 MySQL 資料庫
docker-compose up -d mysql

# 等待資料庫就緒（約 30 秒）
docker-compose logs -f mysql
```

### 3. 設定環境變數

複製範例設定檔：

```bash
cp src/LineNotify.Api/appsettings.Development.example.json \
   src/LineNotify.Api/appsettings.Development.json
```

編輯 `appsettings.Development.json`，填入 Line API 設定：

```json
{
  "Line": {
    "ChannelId": "你的 Line Login Channel ID",
    "ChannelSecret": "你的 Line Login Channel Secret",
    "MessagingChannelAccessToken": "你的 Messaging API Channel Access Token"
  }
}
```

### 4. 執行資料庫遷移

```bash
cd src/LineNotify.Api
dotnet ef database update
```

### 5. 啟動應用程式

```bash
dotnet run
```

應用程式將在 `https://localhost:5001` 啟動。

### 6. 首次登入

1. 開啟瀏覽器訪問 `https://localhost:5001/admin`
2. 使用預設管理員帳號登入：
   - 帳號：`ADMIN`
   - 密碼：`ADMIN`
3. 系統會要求您修改密碼

## 使用 Docker Compose 完整啟動

```bash
# 建置並啟動所有服務
docker-compose up --build -d

# 查看日誌
docker-compose logs -f api

# 停止服務
docker-compose down
```

## 測試 API

### 建立 API Key

登入管理後台後，前往「API Key 管理」建立新的 API Key。

### 發送測試訊息

```bash
curl -X POST https://localhost:5001/api/v1/messages/send \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "messageType": "INFO",
    "title": "測試訊息",
    "content": "這是一則測試訊息",
    "source": {
      "host": "test-server",
      "service": "test"
    }
  }'
```

## Line 設定指南

### Line Login Channel

1. 前往 [Line Developers Console](https://developers.line.biz/)
2. 建立新的 Provider（如果沒有的話）
3. 建立新的 **Line Login** Channel
4. 設定 Callback URL: `https://your-domain/api/v1/auth/line/callback`
5. 記錄 Channel ID 和 Channel Secret

### Messaging API Channel

1. 在同一個 Provider 下建立 **Messaging API** Channel
2. 啟用「使用 Webhook」
3. 設定 Webhook URL: `https://your-domain/api/v1/line/webhook`
4. 發行 Channel Access Token（長期）
5. 記錄 Channel Access Token

### 綁定官方帳號

使用者需要先加入您的 Line 官方帳號為好友，才能接收訊息通知。

## 目錄結構

```
NotifySystem/
├── src/
│   ├── LineNotify.Api/           # 主要 API 專案
│   └── LineNotify.Tests/         # 測試專案
├── docker/
│   ├── docker-compose.yml        # 開發環境
│   └── docker-compose.prod.yml   # 生產環境
├── docs/                         # 文件
└── specs/                        # 規格文件
```

## 常見問題

### Q: Line Login 回調失敗？

確認 Callback URL 已正確設定在 Line Developers Console，且使用 HTTPS。

### Q: 訊息發送失敗？

1. 確認使用者已加入 Line 官方帳號為好友
2. 檢查 Channel Access Token 是否有效
3. 查看應用程式日誌獲取詳細錯誤訊息

### Q: 資料庫連線失敗？

確認 MySQL 容器已啟動且健康：

```bash
docker-compose ps
docker-compose logs mysql
```
