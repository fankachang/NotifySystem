using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 管理員詳細回應（列表使用）
/// </summary>
public class AdminListResponse
{
    /// <summary>管理員 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>帳號名稱</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>顯示名稱</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>是否為超級管理員</summary>
    [JsonPropertyName("isSuperAdmin")]
    public bool IsSuperAdmin { get; set; }

    /// <summary>是否啟用</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>需要修改密碼</summary>
    [JsonPropertyName("mustChangePassword")]
    public bool MustChangePassword { get; set; }

    /// <summary>連結的 Line 用戶</summary>
    [JsonPropertyName("linkedUser")]
    public SimpleUserResponse? LinkedUser { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>最後登入時間</summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}
