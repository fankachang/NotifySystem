using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// Line Messaging 服務介面
/// 負責與 Line Messaging API 互動，發送訊息給使用者
/// </summary>
public interface ILineMessagingService
{
    /// <summary>
    /// 發送推播訊息給單一使用者
    /// </summary>
    /// <param name="lineUserId">Line 使用者 ID</param>
    /// <param name="message">訊息內容</param>
    /// <returns>發送結果</returns>
    Task<LineMessageResult> SendPushMessageAsync(string lineUserId, LineMessageContent message);

    /// <summary>
    /// 發送推播訊息給多個使用者
    /// </summary>
    /// <param name="lineUserIds">Line 使用者 ID 列表（最多 500 個）</param>
    /// <param name="message">訊息內容</param>
    /// <returns>發送結果</returns>
    Task<LineMessageResult> SendMulticastMessageAsync(IEnumerable<string> lineUserIds, LineMessageContent message);

    /// <summary>
    /// 發送 Flex Message 給單一使用者
    /// </summary>
    /// <param name="lineUserId">Line 使用者 ID</param>
    /// <param name="alertMessage">告警訊息內容</param>
    /// <returns>發送結果</returns>
    Task<LineMessageResult> SendAlertFlexMessageAsync(string lineUserId, AlertMessageContent alertMessage);

    /// <summary>
    /// 發送 Flex Message 給多個使用者
    /// </summary>
    /// <param name="lineUserIds">Line 使用者 ID 列表</param>
    /// <param name="alertMessage">告警訊息內容</param>
    /// <returns>發送結果</returns>
    Task<LineMessageResult> SendMulticastAlertFlexMessageAsync(IEnumerable<string> lineUserIds, AlertMessageContent alertMessage);

    /// <summary>
    /// 檢查使用者的 Line 綁定是否有效
    /// </summary>
    /// <param name="lineUserId">Line 使用者 ID</param>
    /// <returns>是否有效</returns>
    Task<bool> IsUserLinkedAsync(string lineUserId);

    /// <summary>
    /// 取得 Line API 剩餘配額
    /// </summary>
    /// <returns>剩餘推播訊息數量</returns>
    Task<int> GetRemainingQuotaAsync();
}

/// <summary>
/// Line 訊息內容
/// </summary>
public class LineMessageContent
{
    /// <summary>訊息類型 (text, flex, image, etc.)</summary>
    public string Type { get; set; } = "text";

    /// <summary>文字訊息內容</summary>
    public string? Text { get; set; }

    /// <summary>替代文字（用於 Flex Message）</summary>
    public string? AltText { get; set; }

    /// <summary>Flex Message 內容 (JSON)</summary>
    public object? Contents { get; set; }
}

/// <summary>
/// 告警訊息內容
/// </summary>
public class AlertMessageContent
{
    /// <summary>訊息類型代碼</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>訊息標題</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>訊息內容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>來源主機</summary>
    public string? SourceHost { get; set; }

    /// <summary>來源服務</summary>
    public string? SourceService { get; set; }

    /// <summary>來源 IP</summary>
    public string? SourceIp { get; set; }

    /// <summary>優先級 (high, normal, low)</summary>
    public string Priority { get; set; } = "normal";

    /// <summary>發送時間</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>擴展資料</summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Line 訊息發送結果
/// </summary>
public class LineMessageResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>錯誤代碼</summary>
    public string? ErrorCode { get; set; }

    /// <summary>錯誤訊息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Request ID (Line 回傳)</summary>
    public string? RequestId { get; set; }

    /// <summary>失敗的使用者 ID 列表</summary>
    public List<string>? FailedUserIds { get; set; }

    /// <summary>建立成功結果</summary>
    public static LineMessageResult Ok(string? requestId = null) => new()
    {
        Success = true,
        RequestId = requestId
    };

    /// <summary>建立失敗結果</summary>
    public static LineMessageResult Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
