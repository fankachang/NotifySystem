using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 審計日誌列表回應
/// </summary>
public class AuditLogListResponse
{
    /// <summary>審計日誌列表</summary>
    [JsonPropertyName("items")]
    public List<AuditLogItem> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// 審計日誌項目
/// </summary>
public class AuditLogItem
{
    /// <summary>日誌 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>操作者（管理員）</summary>
    [JsonPropertyName("admin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SimpleAdminResponse? Admin { get; set; }

    /// <summary>操作類型</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>實體類型</summary>
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>實體 ID</summary>
    [JsonPropertyName("entityId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EntityId { get; set; }

    /// <summary>舊值</summary>
    [JsonPropertyName("oldValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? OldValue { get; set; }

    /// <summary>新值</summary>
    [JsonPropertyName("newValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? NewValue { get; set; }

    /// <summary>IP 位址</summary>
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>操作時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 簡化管理員資訊
/// </summary>
public class SimpleAdminResponse
{
    /// <summary>管理員 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>帳號名稱</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}
