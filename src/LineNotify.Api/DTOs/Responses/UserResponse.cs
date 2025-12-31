using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 使用者資訊回應
/// </summary>
public class UserResponse
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

    /// <summary>是否為管理員</summary>
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    /// <summary>是否為新使用者（首次登入）</summary>
    [JsonPropertyName("isNewUser")]
    public bool IsNewUser { get; set; }

    /// <summary>所屬群組列表</summary>
    [JsonPropertyName("groups")]
    public List<SimpleGroupResponse> Groups { get; set; } = new();

    /// <summary>訂閱的訊息類型列表</summary>
    [JsonPropertyName("subscribedMessageTypes")]
    public List<SimpleMessageTypeResponse> SubscribedMessageTypes { get; set; } = new();

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>最後登入時間</summary>
    [JsonPropertyName("lastLoginAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// 簡化群組資訊（用於關聯顯示）
/// </summary>
public class SimpleGroupResponse
{
    /// <summary>群組 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>群組代碼</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>群組名稱</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 簡化訊息類型資訊（用於關聯顯示）
/// </summary>
public class SimpleMessageTypeResponse
{
    /// <summary>訊息類型 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>訊息類型代碼</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>訊息類型名稱</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 訊息類型回應
/// </summary>
public class MessageTypeResponse
{
    /// <summary>訊息類型 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>類型代碼</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>類型名稱</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>類型說明</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 當前使用者資訊回應（GET /auth/me）
/// </summary>
public class CurrentUserResponse
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

    /// <summary>是否為管理員</summary>
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    /// <summary>所屬群組列表</summary>
    [JsonPropertyName("groups")]
    public List<SimpleGroupResponse> Groups { get; set; } = new();

    /// <summary>訂閱的訊息類型列表</summary>
    [JsonPropertyName("subscribedMessageTypes")]
    public List<SimpleMessageTypeResponse> SubscribedMessageTypes { get; set; } = new();

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>最後登入時間</summary>
    [JsonPropertyName("lastLoginAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastLoginAt { get; set; }
}
