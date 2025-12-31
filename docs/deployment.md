# 部署指南

本文件說明如何部署 Line 通知服務到生產環境。

## 系統需求

### 硬體需求

- **CPU**: 2 核心以上
- **記憶體**: 4GB 以上
- **硬碟**: 20GB 以上（建議 SSD）

### 軟體需求

- **作業系統**: Linux（建議 Ubuntu 22.04 LTS）
- **Docker**: 24.0+
- **Docker Compose**: 2.20+

---

## 快速部署

### 1. 取得專案

```bash
git clone https://github.com/your-org/line-notify-service.git
cd line-notify-service
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

```bash
cd docker
docker-compose -f docker-compose.prod.yml up -d
```

### 4. 驗證部署

```bash
# 檢查服務狀態
docker-compose -f docker-compose.prod.yml ps

# 檢查健康狀態
curl http://localhost:8080/health

# 查看日誌
docker-compose -f docker-compose.prod.yml logs -f api
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

### Nginx 反向代理（建議）

如果需要使用 HTTPS，建議使用 Nginx 作為反向代理：

```bash
# 啟動包含 Nginx 的配置
docker-compose -f docker-compose.prod.yml --profile with-nginx up -d
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
# 進入 API 容器
docker exec -it linenotify-api /bin/bash

# 執行遷移
dotnet ef database update
```

### 資料庫備份

備份服務會每天凌晨 3 點自動執行備份，備份檔案保留 7 天。

手動執行備份：

```bash
docker exec -it linenotify-mysql-backup /backup.sh
```

查看備份檔案：

```bash
docker exec -it linenotify-mysql-backup ls -la /backup
```

還原備份：

```bash
# 解壓縮備份檔案
gunzip linenotify_20250101_030000.sql.gz

# 還原
docker exec -i linenotify-mysql mysql -u linenotify -p linenotify < linenotify_20250101_030000.sql
```

---

## 監控與日誌

### 日誌查看

```bash
# API 服務日誌
docker logs -f linenotify-api

# MySQL 日誌
docker logs -f linenotify-mysql

# 所有服務日誌
docker-compose -f docker-compose.prod.yml logs -f
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
docker-compose -f docker-compose.prod.yml logs api
```

### 資料庫連線失敗

1. 確認 MySQL 容器正常運行
2. 檢查連線字串
3. 確認防火牆設定

```bash
# 測試資料庫連線
docker exec -it linenotify-mysql mysql -u linenotify -p
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

# 重新建置並部署
cd docker
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
```

### 回滾

```bash
# 切換到之前的版本
git checkout <previous-tag>

# 重新部署
docker-compose -f docker-compose.prod.yml up -d --build
```

---

## 安全建議

1. **使用強密碼**: 所有密碼應至少 16 字元，包含大小寫字母、數字和特殊符號
2. **定期更新**: 定期更新 Docker 映像和系統套件
3. **限制網路存取**: 使用防火牆限制只允許必要的連接埠
4. **啟用 HTTPS**: 生產環境必須使用 HTTPS
5. **定期備份**: 確保自動備份正常運作
6. **監控告警**: 設定系統監控和告警通知
