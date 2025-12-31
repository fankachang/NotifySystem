-- Line 通知服務 - MySQL 初始化腳本
-- 此腳本在 MySQL 容器首次啟動時執行

-- 設定字元集
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- 確保資料庫存在（通常已由 MYSQL_DATABASE 環境變數建立）
CREATE DATABASE IF NOT EXISTS linenotify
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE linenotify;

-- 授予使用者完整權限
GRANT ALL PRIVILEGES ON linenotify.* TO 'linenotify'@'%';
FLUSH PRIVILEGES;

-- 注意：資料表結構將由 Entity Framework Core 遷移自動建立
-- 此腳本僅用於額外的初始化設定

-- 建立事件排程器（用於定時任務，如 90 天資料清理）
SET GLOBAL event_scheduler = ON;

-- 如果需要手動建立清理事件（EF Core 遷移會處理資料表結構）
-- 這裡只是示範，實際清理邏輯由 BackgroundService 處理
-- DELIMITER //
-- CREATE EVENT IF NOT EXISTS cleanup_old_messages
-- ON SCHEDULE EVERY 1 DAY
-- STARTS CURRENT_TIMESTAMP
-- DO
-- BEGIN
--     DELETE FROM MessageDeliveries WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY);
--     DELETE FROM Messages WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY);
--     DELETE FROM LoginLogs WHERE LoginAt < DATE_SUB(NOW(), INTERVAL 90 DAY);
-- END //
-- DELIMITER ;
