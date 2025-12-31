using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// Token 回應
/// </summary>
public class TokenResponse
{
    /// <summary>JWT Token</summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Refresh Token</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Token 有效期（秒）</summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// 使用者登入回應（包含 Token 和使用者資訊）
/// </summary>
public class UserLoginResponse
{
    /// <summary>JWT Token</summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Refresh Token</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Token 有效期（秒）</summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    /// <summary>使用者資訊</summary>
    [JsonPropertyName("user")]
    public UserResponse User { get; set; } = null!;
}

/// <summary>
/// 管理員登入回應（包含 Token 和管理員資訊）
/// </summary>
public class AdminLoginResponse
{
    /// <summary>JWT Token</summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Refresh Token</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Token 有效期（秒）</summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    /// <summary>管理員資訊</summary>
    [JsonPropertyName("admin")]
    public AdminResponse Admin { get; set; } = null!;
}

/// <summary>
/// Line Login 授權 URL 回應
/// </summary>
public class LineLoginUrlResponse
{
    /// <summary>授權 URL</summary>
    [JsonPropertyName("authUrl")]
    public string AuthUrl { get; set; } = string.Empty;
}

/// <summary>
/// 管理員資訊回應
/// </summary>
public class AdminResponse
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

    /// <summary>是否需要修改密碼</summary>
    [JsonPropertyName("mustChangePassword")]
    public bool MustChangePassword { get; set; }

    /// <summary>關聯的 Line 使用者</summary>
    [JsonPropertyName("linkedUser")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SimpleUserResponse? LinkedUser { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>最後登入時間</summary>
    [JsonPropertyName("lastLoginAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// 簡化使用者資訊（用於關聯顯示）
/// </summary>
public class SimpleUserResponse
{
    /// <summary>使用者 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>顯示名稱</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
}
