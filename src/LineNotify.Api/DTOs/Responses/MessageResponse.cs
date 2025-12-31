using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 發送訊息回應
/// </summary>
public class SendMessageResponse
{
    /// <summary>訊息 ID</summary>
    [JsonPropertyName("messageId")]
    public int MessageId { get; set; }

    /// <summary>接收者數量</summary>
    [JsonPropertyName("recipientCount")]
    public int RecipientCount { get; set; }

    /// <summary>狀態 (queued, processing, completed)</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "queued";
}

/// <summary>
/// 訊息詳細回應
/// </summary>
public class MessageResponse
{
    /// <summary>訊息 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>訊息類型代碼</summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>訊息標題</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>訊息內容</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>來源主機</summary>
    [JsonPropertyName("sourceHost")]
    public string? SourceHost { get; set; }

    /// <summary>來源服務</summary>
    [JsonPropertyName("sourceService")]
    public string? SourceService { get; set; }

    /// <summary>狀態</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>接收者數量</summary>
    [JsonPropertyName("recipientCount")]
    public int RecipientCount { get; set; }

    /// <summary>已發送數量</summary>
    [JsonPropertyName("sentCount")]
    public int SentCount { get; set; }

    /// <summary>失敗數量</summary>
    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>完成時間</summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 訊息列表項目
/// </summary>
public class MessageListItem
{
    /// <summary>訊息 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>訊息類型代碼</summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>訊息標題</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>來源主機</summary>
    [JsonPropertyName("sourceHost")]
    public string? SourceHost { get; set; }

    /// <summary>接收者數量</summary>
    [JsonPropertyName("recipientCount")]
    public int RecipientCount { get; set; }

    /// <summary>已發送數量</summary>
    [JsonPropertyName("sentCount")]
    public int SentCount { get; set; }

    /// <summary>失敗數量</summary>
    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 使用者訊息項目
/// </summary>
public class UserMessageItem
{
    /// <summary>訊息 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>訊息類型代碼</summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>訊息標題</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>訊息內容</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>來源主機</summary>
    [JsonPropertyName("sourceHost")]
    public string? SourceHost { get; set; }

    /// <summary>發送狀態</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>發送時間</summary>
    [JsonPropertyName("sentAt")]
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// 訊息列表回應（分頁）
/// </summary>
public class MessageListResponse
{
    /// <summary>訊息列表</summary>
    [JsonPropertyName("items")]
    public List<MessageListItem> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// 使用者訊息列表回應（分頁）
/// </summary>
public class UserMessageListResponse
{
    /// <summary>訊息列表</summary>
    [JsonPropertyName("items")]
    public List<UserMessageItem> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}
