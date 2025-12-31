using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 訊息派送服務介面
/// 負責處理訊息的發送邏輯、訂閱者查詢、去重等
/// </summary>
public interface IMessageDispatchService
{
    /// <summary>
    /// 建立訊息並派送給訂閱者
    /// </summary>
    /// <param name="request">發送訊息請求</param>
    /// <param name="apiKeyId">API Key ID (如果透過 API Key 發送)</param>
    /// <returns>發送結果</returns>
    Task<MessageDispatchResult> DispatchMessageAsync(SendMessageRequest request, int? apiKeyId = null);

    /// <summary>
    /// 取得訊息詳情
    /// </summary>
    /// <param name="messageId">訊息 ID</param>
    /// <returns>訊息詳情</returns>
    Task<Message?> GetMessageByIdAsync(int messageId);

    /// <summary>
    /// 查詢訊息列表（分頁）
    /// </summary>
    /// <param name="query">查詢條件</param>
    /// <returns>訊息列表</returns>
    Task<MessageListResponse> GetMessagesAsync(MessageQueryRequest query);

    /// <summary>
    /// 查詢使用者收到的訊息（分頁）
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="query">查詢條件</param>
    /// <returns>使用者訊息列表</returns>
    Task<UserMessageListResponse> GetUserMessagesAsync(int userId, MessageQueryRequest query);

    /// <summary>
    /// 查詢訊息的訂閱者
    /// </summary>
    /// <param name="messageTypeCode">訊息類型代碼</param>
    /// <param name="targetGroups">指定群組（可選）</param>
    /// <param name="sourceHost">來源主機（用於篩選）</param>
    /// <param name="sourceService">來源服務（用於篩選）</param>
    /// <returns>符合條件的使用者列表（去重）</returns>
    Task<List<User>> GetSubscribersAsync(string messageTypeCode, List<string>? targetGroups, string? sourceHost, string? sourceService);

    /// <summary>
    /// 檢查是否為重複訊息（抑制機制）
    /// </summary>
    /// <param name="messageTypeCode">訊息類型代碼</param>
    /// <param name="sourceHost">來源主機</param>
    /// <param name="sourceService">來源服務</param>
    /// <param name="windowMinutes">檢查時間窗口（分鐘）</param>
    /// <returns>是否為重複訊息</returns>
    Task<bool> IsDuplicateMessageAsync(string messageTypeCode, string? sourceHost, string? sourceService, int windowMinutes = 5);

    /// <summary>
    /// 重新發送失敗的訊息派送
    /// </summary>
    /// <param name="deliveryId">派送記錄 ID</param>
    /// <returns>發送結果</returns>
    Task<LineMessageResult> RetryDeliveryAsync(int deliveryId);

    /// <summary>
    /// 取得待發送的派送記錄
    /// </summary>
    /// <param name="maxCount">最大數量</param>
    /// <returns>待發送的派送記錄列表</returns>
    Task<List<MessageDelivery>> GetPendingDeliveriesAsync(int maxCount = 100);

    /// <summary>
    /// 更新派送記錄狀態
    /// </summary>
    /// <param name="deliveryId">派送記錄 ID</param>
    /// <param name="status">新狀態</param>
    /// <param name="errorMessage">錯誤訊息（如果有）</param>
    Task UpdateDeliveryStatusAsync(int deliveryId, string status, string? errorMessage = null);
}

/// <summary>
/// 訊息派送結果
/// </summary>
public class MessageDispatchResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>訊息 ID</summary>
    public int MessageId { get; set; }

    /// <summary>接收者數量</summary>
    public int RecipientCount { get; set; }

    /// <summary>狀態 (queued, processing, completed, failed)</summary>
    public string Status { get; set; } = "queued";

    /// <summary>錯誤代碼</summary>
    public string? ErrorCode { get; set; }

    /// <summary>錯誤訊息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>建立成功結果</summary>
    public static MessageDispatchResult Ok(int messageId, int recipientCount) => new()
    {
        Success = true,
        MessageId = messageId,
        RecipientCount = recipientCount,
        Status = recipientCount > 0 ? "queued" : "completed"
    };

    /// <summary>建立失敗結果</summary>
    public static MessageDispatchResult Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Status = "failed"
    };
}
