# 部署指南

本文件說明如何部署 Line 通知服務到生產環境。

## 系統需求

### 硬體需求

- **CPU**: 2 核心以上
- **記憶體**: 4GB 以上
- **硬碟**: 20GB 以上（建議 SSD）

### 軟體需求

- **作業系統**: Linux（建議 Ubuntu 22.04 LTS）
- **容器引擎**（二擇一）:
  - **Docker**: 24.0+ 搭配 Docker Compose 2.20+
  - **Podman**: 4.1+ （內建 compose 支援）

---

## 快速部署

### 1. 取得專案

```bash
git clone https://github.com/fankachang/NotifySystem.git
cd NotifySystem
```

### 2. 配置環境變數

複製範例環境變數檔案：

```bash
cp docker/.env.example docker/.env
```

編輯 `.env` 檔案，填入必要的配置：

```bash
# 資料庫配置
MYSQL_ROOT_PASSWORD=your_secure_root_password
MYSQL_DATABASE=linenotify
MYSQL_USER=linenotify
MYSQL_PASSWORD=your_secure_password

# JWT 配置
JWT_SECRET=your_32_character_or_longer_secret_key

# Line 配置
LINE_CHANNEL_ID=your_line_channel_id
LINE_CHANNEL_SECRET=your_line_channel_secret
LINE_CALLBACK_URL=https://your-domain.com/api/v1/auth/line/callback
LINE_MESSAGING_ACCESS_TOKEN=your_line_messaging_access_token
```

### 3. 啟動服務

使用 Docker：
```bash
cd docker
docker compose -f docker-compose.prod.yml up -d
```

或使用 Podman：
```bash
cd docker
podman compose -f docker-compose.prod.yml up -d
```

### 4. 驗證部署

```bash
# 檢查服務狀態（Docker）
docker compose -f docker-compose.prod.yml ps
# 或 Podman
podman compose -f docker-compose.prod.yml ps

# 檢查健康狀態
curl http://localhost:8080/health

# 查看日誌（Docker）
docker compose -f docker-compose.prod.yml logs -f api
# 或 Podman
podman compose -f docker-compose.prod.yml logs -f api
```

---

## 詳細配置

### Line 開發者帳號設定

1. 前往 [Line Developers Console](https://developers.line.biz/console/)
2. 建立 Provider（如果還沒有）
3. 建立 Line Login Channel
   - 設定 Callback URL: `https://your-domain.com/api/v1/auth/line/callback`
   - 取得 Channel ID 和 Channel Secret
4. 建立 Messaging API Channel
   - 啟用 Webhook（如需接收訊息）
   - 取得 Channel Access Token

---

## 本地開發 - 使用 ngrok 測試 LINE Login

在本地開發環境中，LINE OAuth 需要 HTTPS 公開網址才能進行回調。使用 ngrok 可以建立一個臨時的公開通道來測試 LINE Login。

### 安裝 ngrok

#### macOS (使用 Homebrew)

```bash
brew install ngrok
```

#### Windows (使用 Chocolatey)

```bash
choco install ngrok
```

#### Linux

```bash
# 下載並解壓縮
curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | \
  sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null && \
  echo "deb https://ngrok-agent.s3.amazonaws.com buster main" | \
  sudo tee /etc/apt/sources.list.d/ngrok.list && \
  sudo apt update && sudo apt install ngrok
```

#### 手動安裝

前往 [ngrok 下載頁面](https://ngrok.com/download) 下載對應系統的版本。

### 設定 ngrok

1. 前往 [ngrok 官網](https://ngrok.com/) 註冊免費帳號
2. 登入後，在 [Your Authtoken](https://dashboard.ngrok.com/get-started/your-authtoken) 頁面取得 authtoken
3. 設定 authtoken：

```bash
ngrok config add-authtoken YOUR_AUTHTOKEN
```

### 啟動 ngrok 通道

1. 先啟動本地 API 服務（預設 port 5050）：

```bash
cd src/LineNotify.Api
dotnet run --urls "http://0.0.0.0:5050"
```

2. 在另一個終端機啟動 ngrok：

```bash
ngrok http 5050
```

3. ngrok 會顯示公開網址，例如：

```
Forwarding   https://abc123.ngrok-free.app -> http://localhost:5050
```

### 設定 LINE Login Callback URL

1. 前往 [Line Developers Console](https://developers.line.biz/console/)
2. 選擇您的 LINE Login Channel
3. 在「LINE Login」標籤中，設定 Callback URL：

```
https://your-ngrok-url.ngrok-free.app/api/v1/auth/line/callback
```

4. 更新 `appsettings.Development.json` 中的設定：

```json
{
  "Line": {
    "ChannelId": "YOUR_CHANNEL_ID",
    "ChannelSecret": "YOUR_CHANNEL_SECRET",
    "CallbackUrl": "https://your-ngrok-url.ngrok-free.app/api/v1/auth/line/callback",
    "MessagingChannelAccessToken": "YOUR_MESSAGING_TOKEN"
  }
}
```

### ngrok 使用注意事項

1. **免費帳號限制**：
   - 每次啟動 ngrok 會產生新的隨機網址（付費帳號可固定網址）
   - 每次網址變更後，需同步更新 LINE Console 的 Callback URL

2. **Session 過期**：
   - 免費帳號的 ngrok session 會在 2 小時後過期
   - 過期後需重新啟動 ngrok

3. **查看 ngrok 狀態**：
   - 瀏覽器開啟 `http://localhost:4040` 可查看 ngrok 的請求日誌和狀態

4. **透過 API 取得當前網址**：

```bash
curl -s http://localhost:4040/api/tunnels | jq -r '.tunnels[0].public_url'
```

### 快速啟動腳本

建立 `start-dev.sh` 腳本一鍵啟動：

```bash
#!/bin/bash

# 啟動 API 服務
cd src/LineNotify.Api
dotnet run --urls "http://0.0.0.0:5050" &
API_PID=$!

# 等待 API 啟動
sleep 5

# 啟動 ngrok
ngrok http 5050 &
NGROK_PID=$!

# 等待 ngrok 啟動
sleep 3

# 顯示 ngrok 網址
NGROK_URL=$(curl -s http://localhost:4040/api/tunnels | jq -r '.tunnels[0].public_url')
echo "======================================"
echo "API 服務已啟動：http://localhost:5050"
echo "ngrok 公開網址：$NGROK_URL"
echo "請將以下 Callback URL 設定到 LINE Console："
echo "$NGROK_URL/api/v1/auth/line/callback"
echo "======================================"

# 等待退出訊號
trap "kill $API_PID $NGROK_PID 2>/dev/null" EXIT
wait
```

### Nginx 反向代理（建議）

如果需要使用 HTTPS，建議使用 Nginx 作為反向代理：

```bash
# 啟動包含 Nginx 的配置（Docker）
docker compose -f docker-compose.prod.yml --profile with-nginx up -d
# 或 Podman
podman compose -f docker-compose.prod.yml --profile with-nginx up -d
```

Nginx 配置範例 (`docker/nginx/nginx.conf`):

```nginx
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:8080;
    }

    server {
        listen 80;
        server_name your-domain.com;
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl;
        server_name your-domain.com;

        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;

        location / {
            proxy_pass http://api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

### SSL 憑證

使用 Let's Encrypt 取得免費 SSL 憑證：

```bash
# 安裝 certbot
apt-get install certbot

# 取得憑證
certbot certonly --standalone -d your-domain.com

# 複製憑證到 nginx 目錄
cp /etc/letsencrypt/live/your-domain.com/fullchain.pem docker/nginx/ssl/
cp /etc/letsencrypt/live/your-domain.com/privkey.pem docker/nginx/ssl/
```

---

## 資料庫管理

### 資料庫遷移

首次部署時，EF Core 會自動建立資料表。如需手動遷移：

```bash
# 進入 API 容器（Docker）
docker exec -it linenotify-api /bin/bash
# 或 Podman
podman exec -it linenotify-api /bin/bash

# 執行遷移
dotnet ef database update
```

### 資料庫備份

備份服務會每天凌晨 3 點自動執行備份，備份檔案保留 7 天。

手動執行備份：

```bash
# Docker
docker exec -it linenotify-mysql-backup /backup.sh
# Podman
podman exec -it linenotify-mysql-backup /backup.sh
```

查看備份檔案：

```bash
# Docker
docker exec -it linenotify-mysql-backup ls -la /backup
# Podman
podman exec -it linenotify-mysql-backup ls -la /backup
```

還原備份：

```bash
# 解壓縮備份檔案
gunzip linenotify_20250101_030000.sql.gz

# 還原（Docker）
docker exec -i linenotify-mysql mysql -u linenotify -p linenotify < linenotify_20250101_030000.sql
# 或 Podman
podman exec -i linenotify-mysql mysql -u linenotify -p linenotify < linenotify_20250101_030000.sql
```

---

## 監控與日誌

### 日誌查看

```bash
# API 服務日誌（Docker / Podman 指令相同）
docker logs -f linenotify-api
podman logs -f linenotify-api

# MySQL 日誌
docker logs -f linenotify-mysql
podman logs -f linenotify-mysql

# 所有服務日誌（Docker）
docker compose -f docker-compose.prod.yml logs -f
# 或 Podman
podman compose -f docker-compose.prod.yml logs -f
```

### 健康檢查

服務提供 `/health` 端點進行健康檢查：

```bash
curl http://localhost:8080/health
```

正常回應：
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0100000"
    }
  }
}
```

### Prometheus 指標（可選）

如需整合 Prometheus 監控，可在 `Program.cs` 中啟用：

```csharp
// 在 builder.Services 區塊加入
builder.Services.AddHealthChecks()
    .AddPrometheusMetrics();
```

---

## 擴展與高可用

### 水平擴展

可以透過 Docker Swarm 或 Kubernetes 進行水平擴展：

```bash
# Docker Swarm 範例
docker service create --replicas 3 --name linenotify-api linenotify-api:latest
```

### 負載平衡

使用 Nginx 進行負載平衡：

```nginx
upstream api {
    least_conn;
    server api1:8080;
    server api2:8080;
    server api3:8080;
}
```

---

## 故障排除

### 服務無法啟動

1. 檢查環境變數是否正確設定
2. 檢查資料庫連線
3. 查看容器日誌

```bash
# Docker
docker compose -f docker-compose.prod.yml logs api
# Podman
podman compose -f docker-compose.prod.yml logs api
```

### 資料庫連線失敗

1. 確認 MySQL 容器正常運行
2. 檢查連線字串
3. 確認防火牆設定

```bash
# 測試資料庫連線（Docker）
docker exec -it linenotify-mysql mysql -u linenotify -p
# 或 Podman
podman exec -it linenotify-mysql mysql -u linenotify -p
```

### Line API 錯誤

1. 確認 Channel ID 和 Secret 正確
2. 確認 Callback URL 已在 Line Console 設定
3. 確認 Channel Access Token 有效

---

## 更新部署

### 更新到新版本

```bash
# 拉取最新代碼
git pull origin main

# 重新建置並部署（Docker）
cd docker
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d

# 或 Podman
cd docker
podman compose -f docker-compose.prod.yml build
podman compose -f docker-compose.prod.yml up -d
```

### 回滾

```bash
# 切換到之前的版本
git checkout <previous-tag>

# 重新部署（Docker）
docker compose -f docker-compose.prod.yml up -d --build
# 或 Podman
podman compose -f docker-compose.prod.yml up -d --build
```

---

## 安全建議

1. **使用強密碼**: 所有密碼應至少 16 字元，包含大小寫字母、數字和特殊符號
2. **定期更新**: 定期更新 Docker 映像和系統套件
3. **限制網路存取**: 使用防火牆限制只允許必要的連接埠
4. **啟用 HTTPS**: 生產環境必須使用 HTTPS
5. **定期備份**: 確保自動備份正常運作
6. **監控告警**: 設定系統監控和告警通知
