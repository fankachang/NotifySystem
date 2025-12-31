# Nagios 整合指南

本文件說明如何將 Nagios 監控系統與 Line 通知服務整合，實現告警訊息自動推送到 Line。

## 概述

整合流程：
1. Nagios 偵測到告警事件
2. 執行通知命令呼叫 Line 通知服務 API
3. Line 通知服務處理並發送 Line 訊息
4. 訂閱者在 Line 收到告警通知

---

## 前置準備

### 1. 取得 API Key

登入 Line 通知服務管理後台，建立新的 API Key：

1. 前往「API Key 管理」頁面
2. 點擊「新增 API Key」
3. 填入名稱（如 "Nagios Integration"）
4. 設定過期時間和 IP 限制（可選）
5. 複製產生的 API Key（只會顯示一次）

### 2. 建立訊息類型

確保已建立對應的訊息類型：

- `CRITICAL` - 嚴重告警
- `WARNING` - 警告
- `OK` / `RECOVERY` - 恢復通知
- `UNKNOWN` - 未知狀態

### 3. 設定群組

建立接收告警的群組，並設定：
- 主機篩選模式（如 `web-*`, `db-*`）
- 服務篩選模式（如 `nginx`, `mysql`）
- 接收時段
- 訂閱的訊息類型

---

## 整合方式

### 方式一：使用 Shell 腳本

#### 建立通知腳本

在 Nagios 伺服器上建立 `/usr/local/nagios/libexec/notify_line.sh`：

```bash
#!/bin/bash

# Line 通知服務配置
LINE_NOTIFY_URL="https://your-domain.com/api/v1/messages/send"
API_KEY="your_api_key_here"

# Nagios 傳入的參數
NOTIFICATION_TYPE=$1
HOST_NAME=$2
HOST_STATE=$3
HOST_OUTPUT=$4
SERVICE_DESC=$5
SERVICE_STATE=$6
SERVICE_OUTPUT=$7

# 判斷是主機告警還是服務告警
if [ -z "$SERVICE_DESC" ]; then
    # 主機告警
    MESSAGE_TYPE="${HOST_STATE}"
    TITLE="${NOTIFICATION_TYPE}: ${HOST_NAME} is ${HOST_STATE}"
    CONTENT="${HOST_OUTPUT}"
    SERVICE_NAME=""
else
    # 服務告警
    MESSAGE_TYPE="${SERVICE_STATE}"
    TITLE="${NOTIFICATION_TYPE}: ${SERVICE_DESC} on ${HOST_NAME}"
    CONTENT="${SERVICE_OUTPUT}"
    SERVICE_NAME="${SERVICE_DESC}"
fi

# 優先級判斷
case "${MESSAGE_TYPE}" in
    CRITICAL|DOWN|UNREACHABLE)
        PRIORITY="high"
        ;;
    WARNING)
        PRIORITY="normal"
        ;;
    *)
        PRIORITY="low"
        ;;
esac

# 建立 JSON 請求
JSON_DATA=$(cat <<EOF
{
    "messageType": "${MESSAGE_TYPE}",
    "title": "${TITLE}",
    "content": "${CONTENT}",
    "priority": "${PRIORITY}",
    "source": {
        "host": "${HOST_NAME}",
        "service": "${SERVICE_NAME}"
    },
    "metadata": {
        "notificationType": "${NOTIFICATION_TYPE}",
        "nagiosTimestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    }
}
EOF
)

# 發送請求
curl -s -X POST "${LINE_NOTIFY_URL}" \
    -H "Content-Type: application/json" \
    -H "X-API-Key: ${API_KEY}" \
    -d "${JSON_DATA}"
```

設定權限：
```bash
chmod +x /usr/local/nagios/libexec/notify_line.sh
chown nagios:nagios /usr/local/nagios/libexec/notify_line.sh
```

#### 設定 Nagios 命令

編輯 `/usr/local/nagios/etc/objects/commands.cfg`：

```cfg
# Line 通知 - 主機告警
define command {
    command_name    notify-host-by-line
    command_line    /usr/local/nagios/libexec/notify_line.sh "$NOTIFICATIONTYPE$" "$HOSTNAME$" "$HOSTSTATE$" "$HOSTOUTPUT$"
}

# Line 通知 - 服務告警
define command {
    command_name    notify-service-by-line
    command_line    /usr/local/nagios/libexec/notify_line.sh "$NOTIFICATIONTYPE$" "$HOSTNAME$" "$HOSTSTATE$" "$HOSTOUTPUT$" "$SERVICEDESC$" "$SERVICESTATE$" "$SERVICEOUTPUT$"
}
```

#### 設定聯絡人

編輯 `/usr/local/nagios/etc/objects/contacts.cfg`：

```cfg
define contact {
    contact_name                    line-notify
    alias                           Line Notification
    service_notification_period     24x7
    host_notification_period        24x7
    service_notification_options    w,u,c,r
    host_notification_options       d,u,r
    service_notification_commands   notify-service-by-line
    host_notification_commands      notify-host-by-line
}

define contactgroup {
    contactgroup_name    line-admins
    alias               Line Notification Group
    members             line-notify
}
```

#### 套用到主機/服務

```cfg
define host {
    use                 linux-server
    host_name           web-server-01
    alias               Web Server 01
    address             192.168.1.100
    contact_groups      line-admins
}

define service {
    use                 generic-service
    host_name           web-server-01
    service_description HTTP
    check_command       check_http
    contact_groups      line-admins
}
```

---

### 方式二：使用 Python 腳本

適合需要更複雜處理邏輯的場景。

#### 安裝依賴

```bash
pip install requests
```

#### 建立腳本

`/usr/local/nagios/libexec/notify_line.py`：

```python
#!/usr/bin/env python3
"""
Nagios to Line Notify Integration
"""

import os
import sys
import json
import requests
from datetime import datetime

# 配置
LINE_NOTIFY_URL = os.environ.get('LINE_NOTIFY_URL', 'https://your-domain.com/api/v1/messages/send')
API_KEY = os.environ.get('LINE_NOTIFY_API_KEY', 'your_api_key_here')

def send_notification(notification_type, host_name, host_state, host_output,
                      service_desc=None, service_state=None, service_output=None):
    """發送通知到 Line 通知服務"""
    
    # 判斷告警類型
    if service_desc:
        message_type = service_state
        title = f"{notification_type}: {service_desc} on {host_name}"
        content = service_output
        service_name = service_desc
    else:
        message_type = host_state
        title = f"{notification_type}: {host_name} is {host_state}"
        content = host_output
        service_name = ""
    
    # 優先級映射
    priority_map = {
        'CRITICAL': 'high',
        'DOWN': 'high',
        'UNREACHABLE': 'high',
        'WARNING': 'normal',
        'OK': 'low',
        'UP': 'low',
        'RECOVERY': 'low',
    }
    priority = priority_map.get(message_type, 'normal')
    
    # 建立請求資料
    data = {
        'messageType': message_type,
        'title': title,
        'content': content,
        'priority': priority,
        'source': {
            'host': host_name,
            'service': service_name,
        },
        'metadata': {
            'notificationType': notification_type,
            'nagiosTimestamp': datetime.utcnow().isoformat() + 'Z',
        }
    }
    
    # 發送請求
    headers = {
        'Content-Type': 'application/json',
        'X-API-Key': API_KEY,
    }
    
    try:
        response = requests.post(LINE_NOTIFY_URL, json=data, headers=headers, timeout=30)
        response.raise_for_status()
        result = response.json()
        print(f"Success: Message ID {result.get('data', {}).get('messageId')}")
        return True
    except requests.exceptions.RequestException as e:
        print(f"Error: {e}", file=sys.stderr)
        return False

if __name__ == '__main__':
    if len(sys.argv) < 5:
        print("Usage: notify_line.py <notification_type> <host_name> <host_state> <host_output> [service_desc] [service_state] [service_output]")
        sys.exit(1)
    
    args = sys.argv[1:8]
    # 補齊參數
    while len(args) < 7:
        args.append(None)
    
    success = send_notification(*args)
    sys.exit(0 if success else 1)
```

---

## 進階設定

### 依主機群組發送

可以根據 Nagios 主機群組對應到不同的 Line 群組：

```bash
# 在腳本中加入目標群組判斷
case "$HOST_NAME" in
    web-*)
        TARGET_GROUPS='["web-team"]'
        ;;
    db-*)
        TARGET_GROUPS='["dba-team"]'
        ;;
    *)
        TARGET_GROUPS='["ops-team"]'
        ;;
esac

# 加入到 JSON 請求
"targetGroups": ${TARGET_GROUPS},
```

### 告警抑制

Line 通知服務內建重複告警抑制機制（預設 5 分鐘內相同告警不重複發送）。

如需調整，可在 `appsettings.json` 中設定：

```json
{
  "App": {
    "AlertSuppressionMinutes": 10
  }
}
```

### 僅發送重要告警

只在 CRITICAL 和 DOWN 狀態時發送：

```cfg
define contact {
    contact_name                    line-critical
    service_notification_options    c       # 只有 Critical
    host_notification_options       d       # 只有 Down
    ...
}
```

---

## 測試整合

### 手動測試

```bash
# 測試主機告警
/usr/local/nagios/libexec/notify_line.sh "PROBLEM" "web-server-01" "DOWN" "Host unreachable"

# 測試服務告警
/usr/local/nagios/libexec/notify_line.sh "PROBLEM" "web-server-01" "UP" "" "HTTP" "CRITICAL" "HTTP CRITICAL - Connection refused"
```

### 使用 curl 直接測試 API

```bash
curl -X POST "https://your-domain.com/api/v1/messages/send" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your_api_key" \
  -d '{
    "messageType": "CRITICAL",
    "title": "測試告警",
    "content": "這是一則測試告警訊息",
    "priority": "high",
    "source": {
      "host": "test-server",
      "service": "test-service"
    }
  }'
```

---

## 故障排除

### 通知未發送

1. **檢查 API Key**: 確認 API Key 正確且未過期
2. **檢查網路連線**: 確認 Nagios 伺服器可以存取 Line 通知服務
3. **檢查腳本權限**: 確認腳本有執行權限
4. **查看 Nagios 日誌**: `/usr/local/nagios/var/nagios.log`

```bash
# 測試網路連線
curl -I https://your-domain.com/health

# 查看最近的通知記錄
grep "notify" /usr/local/nagios/var/nagios.log | tail -20
```

### 收到重複通知

1. 檢查 Nagios 通知間隔設定
2. 確認 Line 通知服務的告警抑制設定
3. 檢查是否有多個聯絡人設定

### Line 訊息格式異常

確認傳入的參數沒有特殊字元導致 JSON 格式錯誤：

```bash
# 使用 jq 驗證 JSON 格式
echo "${JSON_DATA}" | jq .
```

---

## 參考資源

- [Nagios 官方文件](https://www.nagios.org/documentation/)
- [Nagios 通知命令](https://assets.nagios.com/downloads/nagioscore/docs/nagioscore/4/en/notifications.html)
- [Line 通知服務 API 文件](./api-reference.md)
