using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LineNotify.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LineNotify.Api.Services;

/// <summary>
/// Line Messaging 服務實作
/// 使用 Line Messaging API 發送訊息
/// </summary>
public class LineMessagingService : ILineMessagingService
{
    private readonly HttpClient _httpClient;
    private readonly LineSettings _settings;
    private readonly ILogger<LineMessagingService> _logger;

    private const string BaseUrl = "https://api.line.me/v2/bot";
    private const int MaxMulticastUsers = 500;

    public LineMessagingService(
        HttpClient httpClient,
        IOptions<LineSettings> settings,
        ILogger<LineMessagingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // 設定 Authorization 標頭
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.MessagingChannelAccessToken);
    }

    /// <inheritdoc />
    public async Task<LineMessageResult> SendPushMessageAsync(string lineUserId, LineMessageContent message)
    {
        var requestBody = new
        {
            to = lineUserId,
            messages = new[] { MapToLineMessage(message) }
        };

        return await SendRequestAsync($"{BaseUrl}/message/push", requestBody);
    }

    /// <inheritdoc />
    public async Task<LineMessageResult> SendMulticastMessageAsync(IEnumerable<string> lineUserIds, LineMessageContent message)
    {
        var userIds = lineUserIds.ToList();

        if (userIds.Count > MaxMulticastUsers)
        {
            _logger.LogWarning("Multicast 使用者數量超過限制: {Count} > {Max}", userIds.Count, MaxMulticastUsers);
        }

        // 分批發送
        var batches = userIds.Chunk(MaxMulticastUsers);
        var failedUserIds = new List<string>();
        string? lastRequestId = null;

        foreach (var batch in batches)
        {
            var requestBody = new
            {
                to = batch,
                messages = new[] { MapToLineMessage(message) }
            };

            var result = await SendRequestAsync($"{BaseUrl}/message/multicast", requestBody);

            if (!result.Success)
            {
                failedUserIds.AddRange(batch);
            }
            else
            {
                lastRequestId = result.RequestId;
            }
        }

        if (failedUserIds.Count > 0)
        {
            return new LineMessageResult
            {
                Success = false,
                ErrorCode = "PARTIAL_FAILURE",
                ErrorMessage = $"部分訊息發送失敗: {failedUserIds.Count} 筆",
                FailedUserIds = failedUserIds
            };
        }

        return LineMessageResult.Ok(lastRequestId);
    }

    /// <inheritdoc />
    public async Task<LineMessageResult> SendAlertFlexMessageAsync(string lineUserId, AlertMessageContent alertMessage)
    {
        var flexMessage = BuildAlertFlexMessage(alertMessage);

        var requestBody = new
        {
            to = lineUserId,
            messages = new[] { flexMessage }
        };

        return await SendRequestAsync($"{BaseUrl}/message/push", requestBody);
    }

    /// <inheritdoc />
    public async Task<LineMessageResult> SendMulticastAlertFlexMessageAsync(IEnumerable<string> lineUserIds, AlertMessageContent alertMessage)
    {
        var userIds = lineUserIds.ToList();
        var flexMessage = BuildAlertFlexMessage(alertMessage);

        // 分批發送
        var batches = userIds.Chunk(MaxMulticastUsers);
        var failedUserIds = new List<string>();
        string? lastRequestId = null;

        foreach (var batch in batches)
        {
            var requestBody = new
            {
                to = batch,
                messages = new[] { flexMessage }
            };

            var result = await SendRequestAsync($"{BaseUrl}/message/multicast", requestBody);

            if (!result.Success)
            {
                failedUserIds.AddRange(batch);
            }
            else
            {
                lastRequestId = result.RequestId;
            }
        }

        if (failedUserIds.Count > 0)
        {
            return new LineMessageResult
            {
                Success = failedUserIds.Count < userIds.Count,
                ErrorCode = failedUserIds.Count == userIds.Count ? "ALL_FAILED" : "PARTIAL_FAILURE",
                ErrorMessage = $"部分訊息發送失敗: {failedUserIds.Count}/{userIds.Count} 筆",
                FailedUserIds = failedUserIds,
                RequestId = lastRequestId
            };
        }

        return LineMessageResult.Ok(lastRequestId);
    }

    /// <inheritdoc />
    public async Task<bool> IsUserLinkedAsync(string lineUserId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/profile/{lineUserId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檢查使用者 Line 綁定狀態失敗: {LineUserId}", lineUserId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetRemainingQuotaAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/message/quota/consumption");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);

                if (data.TryGetProperty("totalUsage", out var totalUsage))
                {
                    // 免費方案每月 200 則，付費方案依方案而定
                    var monthlyLimit = 200; // 可從設定讀取
                    return monthlyLimit - totalUsage.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得 Line API 配額失敗");
        }

        return -1; // 表示無法取得
    }

    #region Private Methods

    /// <summary>
    /// 發送 HTTP 請求到 Line API
    /// </summary>
    private async Task<LineMessageResult> SendRequestAsync(string url, object requestBody)
    {
        try
        {
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            // 取得 Request ID
            var requestId = response.Headers.TryGetValues("X-Line-Request-Id", out var values)
                ? values.FirstOrDefault()
                : null;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Line API 請求成功: {Url}, RequestId: {RequestId}", url, requestId);
                return LineMessageResult.Ok(requestId);
            }

            // 解析錯誤回應
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorData = JsonSerializer.Deserialize<JsonElement>(errorContent);

            var errorMessage = errorData.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "未知錯誤"
                : "未知錯誤";

            _logger.LogWarning("Line API 請求失敗: {StatusCode}, {ErrorMessage}, RequestId: {RequestId}",
                response.StatusCode, errorMessage, requestId);

            return LineMessageResult.Fail(
                response.StatusCode.ToString(),
                errorMessage
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Line API 網路請求失敗: {Url}", url);
            return LineMessageResult.Fail("NETWORK_ERROR", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Line API 請求逾時: {Url}", url);
            return LineMessageResult.Fail("TIMEOUT", "請求逾時");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Line API 請求發生錯誤: {Url}", url);
            return LineMessageResult.Fail("INTERNAL_ERROR", ex.Message);
        }
    }

    /// <summary>
    /// 將訊息內容映射為 Line 訊息格式
    /// </summary>
    private static object MapToLineMessage(LineMessageContent message)
    {
        if (message.Type == "text")
        {
            return new
            {
                type = "text",
                text = TruncateText(message.Text ?? "", 5000)
            };
        }

        if (message.Type == "flex" && message.Contents != null)
        {
            return new
            {
                type = "flex",
                altText = TruncateText(message.AltText ?? "通知訊息", 400),
                contents = message.Contents
            };
        }

        // 預設為文字訊息
        return new
        {
            type = "text",
            text = TruncateText(message.Text ?? "", 5000)
        };
    }

    /// <summary>
    /// 建立告警 Flex Message
    /// </summary>
    private static object BuildAlertFlexMessage(AlertMessageContent alert)
    {
        var headerColor = alert.MessageType.ToUpperInvariant() switch
        {
            "CRITICAL" => "#DC3545",  // 紅色
            "WARNING" => "#FFC107",   // 黃色
            "INFO" => "#17A2B8",      // 藍色
            "OK" or "RECOVERY" => "#28A745", // 綠色
            _ => "#6C757D"            // 灰色
        };

        var priorityEmoji = alert.Priority switch
        {
            "high" => "🔴",
            "low" => "🟢",
            _ => "🟡"
        };

        var contents = new
        {
            type = "bubble",
            size = "mega",
            header = new
            {
                type = "box",
                layout = "vertical",
                backgroundColor = headerColor,
                paddingAll = "lg",
                contents = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"{priorityEmoji} [{alert.MessageType}]",
                        color = "#FFFFFF",
                        size = "sm",
                        weight = "bold"
                    }
                }
            },
            body = new
            {
                type = "box",
                layout = "vertical",
                spacing = "md",
                paddingAll = "lg",
                contents = BuildBodyContents(alert)
            },
            footer = new
            {
                type = "box",
                layout = "vertical",
                paddingAll = "sm",
                contents = new[]
                {
                    new
                    {
                        type = "text",
                        text = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        color = "#888888",
                        size = "xs",
                        align = "end"
                    }
                }
            }
        };

        return new
        {
            type = "flex",
            altText = TruncateText($"[{alert.MessageType}] {alert.Title}", 400),
            contents
        };
    }

    /// <summary>
    /// 建立 Flex Message Body 內容
    /// </summary>
    private static object[] BuildBodyContents(AlertMessageContent alert)
    {
        var contents = new List<object>
        {
            // 標題
            new
            {
                type = "text",
                text = TruncateText(alert.Title, 100),
                weight = "bold",
                size = "lg",
                wrap = true
            },
            // 分隔線
            new
            {
                type = "separator",
                margin = "md"
            },
            // 內容
            new
            {
                type = "text",
                text = TruncateText(alert.Content, 500),
                size = "sm",
                wrap = true,
                margin = "md"
            }
        };

        // 來源資訊
        if (!string.IsNullOrEmpty(alert.SourceHost) || !string.IsNullOrEmpty(alert.SourceService))
        {
            contents.Add(new
            {
                type = "separator",
                margin = "md"
            });

            if (!string.IsNullOrEmpty(alert.SourceHost))
            {
                contents.Add(new
                {
                    type = "box",
                    layout = "horizontal",
                    margin = "sm",
                    contents = new object[]
                    {
                        new { type = "text", text = "主機:", size = "xs", color = "#888888", flex = 2 },
                        new { type = "text", text = alert.SourceHost, size = "xs", wrap = true, flex = 5 }
                    }
                });
            }

            if (!string.IsNullOrEmpty(alert.SourceService))
            {
                contents.Add(new
                {
                    type = "box",
                    layout = "horizontal",
                    margin = "sm",
                    contents = new object[]
                    {
                        new { type = "text", text = "服務:", size = "xs", color = "#888888", flex = 2 },
                        new { type = "text", text = alert.SourceService, size = "xs", wrap = true, flex = 5 }
                    }
                });
            }

            if (!string.IsNullOrEmpty(alert.SourceIp))
            {
                contents.Add(new
                {
                    type = "box",
                    layout = "horizontal",
                    margin = "sm",
                    contents = new object[]
                    {
                        new { type = "text", text = "IP:", size = "xs", color = "#888888", flex = 2 },
                        new { type = "text", text = alert.SourceIp, size = "xs", flex = 5 }
                    }
                });
            }
        }

        return contents.ToArray();
    }

    /// <summary>
    /// 截斷文字
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
