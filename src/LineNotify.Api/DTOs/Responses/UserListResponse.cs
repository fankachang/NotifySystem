using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 使用者列表回應
/// </summary>
public class UserListResponse
{
    /// <summary>使用者列表</summary>
    [JsonPropertyName("items")]
    public List<UserListItem> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// 使用者列表項目
/// </summary>
public class UserListItem
{
    /// <summary>使用者 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>顯示名稱</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>頭像 URL</summary>
    [JsonPropertyName("avatarUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; set; }

    /// <summary>Email</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    /// <summary>是否啟用</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>是否為管理員</summary>
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    /// <summary>Line 綁定是否有效</summary>
    [JsonPropertyName("lineBindingValid")]
    public bool LineBindingValid { get; set; }

    /// <summary>所屬群組</summary>
    [JsonPropertyName("groups")]
    public List<SimpleGroupResponse> Groups { get; set; } = new();

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>最後登入時間</summary>
    [JsonPropertyName("lastLoginAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// 更新使用者請求
/// </summary>
public class UpdateUserRequest
{
    /// <summary>顯示名稱</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>是否啟用</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    /// <summary>是否為管理員</summary>
    [JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; set; }
}
