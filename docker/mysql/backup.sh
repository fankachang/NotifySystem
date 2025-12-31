#!/bin/bash
# MySQL 資料庫自動備份腳本
# 此腳本會在指定時間執行備份，並保留最近 7 天的備份檔案

set -e

# 配置變數
BACKUP_DIR="/backup"
MYSQL_HOST="${MYSQL_HOST:-localhost}"
MYSQL_DATABASE="${MYSQL_DATABASE:-linenotify}"
MYSQL_USER="${MYSQL_USER:-linenotify}"
MYSQL_PASSWORD="${MYSQL_PASSWORD}"
RETENTION_DAYS="${RETENTION_DAYS:-7}"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/${MYSQL_DATABASE}_${DATE}.sql.gz"

# 檢查備份目錄
if [ ! -d "$BACKUP_DIR" ]; then
    mkdir -p "$BACKUP_DIR"
fi

echo "[$(date)] 開始備份資料庫: ${MYSQL_DATABASE}"

# 執行備份
mysqldump \
    --host="${MYSQL_HOST}" \
    --user="${MYSQL_USER}" \
    --password="${MYSQL_PASSWORD}" \
    --single-transaction \
    --routines \
    --triggers \
    --events \
    --add-drop-table \
    --create-options \
    --extended-insert \
    --quick \
    "${MYSQL_DATABASE}" | gzip > "${BACKUP_FILE}"

# 檢查備份是否成功
if [ $? -eq 0 ]; then
    BACKUP_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)
    echo "[$(date)] 備份完成: ${BACKUP_FILE} (${BACKUP_SIZE})"
else
    echo "[$(date)] 備份失敗!"
    exit 1
fi

# 清理過期備份
echo "[$(date)] 清理超過 ${RETENTION_DAYS} 天的備份檔案..."
find "${BACKUP_DIR}" -name "${MYSQL_DATABASE}_*.sql.gz" -mtime +${RETENTION_DAYS} -delete

# 列出現有備份
echo "[$(date)] 現有備份檔案:"
ls -lh "${BACKUP_DIR}"/${MYSQL_DATABASE}_*.sql.gz 2>/dev/null || echo "無備份檔案"

echo "[$(date)] 備份作業完成"
