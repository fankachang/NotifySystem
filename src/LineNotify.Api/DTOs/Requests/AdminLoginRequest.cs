using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 管理員登入請求
/// </summary>
public class AdminLoginRequest
{
    /// <summary>帳號名稱</summary>
    [Required(ErrorMessage = "帳號為必填")]
    [MaxLength(50, ErrorMessage = "帳號不可超過 50 字元")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>密碼</summary>
    [Required(ErrorMessage = "密碼為必填")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 管理員修改密碼請求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>目前密碼</summary>
    [Required(ErrorMessage = "目前密碼為必填")]
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>新密碼</summary>
    [Required(ErrorMessage = "新密碼為必填")]
    [MinLength(8, ErrorMessage = "密碼至少需要 8 個字元")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "密碼必須包含至少一個大寫字母、一個小寫字母和一個數字")]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>確認新密碼</summary>
    [Required(ErrorMessage = "確認密碼為必填")]
    [Compare(nameof(NewPassword), ErrorMessage = "確認密碼與新密碼不符")]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 建立管理員請求
/// </summary>
public class CreateAdminRequest
{
    /// <summary>帳號名稱</summary>
    [Required(ErrorMessage = "帳號為必填")]
    [MaxLength(50, ErrorMessage = "帳號不可超過 50 字元")]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9_-]*$", ErrorMessage = "帳號必須以字母開頭，只能包含字母、數字、底線和連字號")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>初始密碼</summary>
    [Required(ErrorMessage = "密碼為必填")]
    [MinLength(8, ErrorMessage = "密碼至少需要 8 個字元")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>顯示名稱</summary>
    [Required(ErrorMessage = "顯示名稱為必填")]
    [MaxLength(100, ErrorMessage = "顯示名稱不可超過 100 字元")]
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>是否為超級管理員</summary>
    [JsonPropertyName("isSuperAdmin")]
    public bool IsSuperAdmin { get; set; } = false;
}

/// <summary>
/// 更新管理員請求
/// </summary>
public class UpdateAdminRequest
{
    /// <summary>顯示名稱</summary>
    [MaxLength(100, ErrorMessage = "顯示名稱不可超過 100 字元")]
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>是否為超級管理員</summary>
    [JsonPropertyName("isSuperAdmin")]
    public bool? IsSuperAdmin { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
