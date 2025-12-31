using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// Line Login 回調請求
/// </summary>
public class LineLoginCallbackRequest
{
    /// <summary>Line OAuth 授權碼</summary>
    [Required(ErrorMessage = "授權碼為必填")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>狀態碼（用於防止 CSRF）</summary>
    [Required(ErrorMessage = "狀態碼為必填")]
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// 刷新 Token 請求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>Refresh Token</summary>
    [Required(ErrorMessage = "Refresh Token 為必填")]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 更新使用者資料請求
/// </summary>
public class UpdateUserProfileRequest
{
    /// <summary>顯示名稱</summary>
    [MaxLength(100, ErrorMessage = "顯示名稱不可超過 100 字元")]
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Email</summary>
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [MaxLength(200, ErrorMessage = "Email 不可超過 200 字元")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
