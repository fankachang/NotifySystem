using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 發送訊息請求
/// </summary>
public class SendMessageRequest
{
    /// <summary>訊息類型代碼 (CRITICAL, WARNING, etc.)</summary>
    [Required(ErrorMessage = "訊息類型為必填")]
    [MaxLength(50, ErrorMessage = "訊息類型代碼不可超過 50 字元")]
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>訊息標題 (最長 200 字元)</summary>
    [Required(ErrorMessage = "訊息標題為必填")]
    [MaxLength(200, ErrorMessage = "訊息標題不可超過 200 字元")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>訊息內容 (最長 2000 字元)</summary>
    [Required(ErrorMessage = "訊息內容為必填")]
    [MaxLength(2000, ErrorMessage = "訊息內容不可超過 2000 字元")]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>來源資訊</summary>
    [JsonPropertyName("source")]
    public MessageSourceInfo? Source { get; set; }

    /// <summary>擴展資料 (任意 JSON)</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>指定發送群組，空陣列或 null 表示所有訂閱者</summary>
    [JsonPropertyName("targetGroups")]
    public List<string>? TargetGroups { get; set; }

    /// <summary>優先級: high, normal, low (預設: normal)</summary>
    [JsonPropertyName("priority")]
    [RegularExpression("^(high|normal|low)$", ErrorMessage = "優先級必須為 high, normal 或 low")]
    public string Priority { get; set; } = "normal";
}

/// <summary>
/// 訊息來源資訊
/// </summary>
public class MessageSourceInfo
{
    /// <summary>來源主機名稱</summary>
    [MaxLength(255)]
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>來源服務名稱</summary>
    [MaxLength(255)]
    [JsonPropertyName("service")]
    public string? Service { get; set; }

    /// <summary>來源 IP 位址</summary>
    [MaxLength(45)]
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }
}

/// <summary>
/// 訊息查詢請求
/// </summary>
public class MessageQueryRequest
{
    /// <summary>頁碼 (預設: 1)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "頁碼必須大於 0")]
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>每頁筆數 (預設: 20, 最大: 100)</summary>
    [Range(1, 100, ErrorMessage = "每頁筆數必須在 1 到 100 之間")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    /// <summary>篩選訊息類型</summary>
    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    /// <summary>篩選來源主機</summary>
    [JsonPropertyName("sourceHost")]
    public string? SourceHost { get; set; }

    /// <summary>篩選來源服務</summary>
    [JsonPropertyName("sourceService")]
    public string? SourceService { get; set; }

    /// <summary>篩選狀態</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>開始日期</summary>
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>結束日期</summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}
