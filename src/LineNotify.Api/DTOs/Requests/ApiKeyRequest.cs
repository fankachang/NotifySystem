using System.ComponentModel.DataAnnotations;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 建立 API Key 請求
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>API Key 名稱（如「Nagios Production」）</summary>
    [Required(ErrorMessage = "名稱為必填")]
    [MaxLength(100, ErrorMessage = "名稱最多 100 字元")]
    public string Name { get; set; } = string.Empty;

    /// <summary>API Key 描述</summary>
    [MaxLength(500, ErrorMessage = "描述最多 500 字元")]
    public string? Description { get; set; }

    /// <summary>過期天數（可為空，表示永不過期）</summary>
    [Range(1, 3650, ErrorMessage = "過期天數必須在 1-3650 之間")]
    public int? ExpiresInDays { get; set; }

    /// <summary>權限設定 - 允許的訊息類型 ID 清單</summary>
    public List<int>? AllowedMessageTypeIds { get; set; }

    /// <summary>權限設定 - 允許的群組 ID 清單</summary>
    public List<int>? AllowedGroupIds { get; set; }
}

/// <summary>
/// 更新 API Key 請求
/// </summary>
public class UpdateApiKeyRequest
{
    /// <summary>API Key 名稱</summary>
    [Required(ErrorMessage = "名稱為必填")]
    [MaxLength(100, ErrorMessage = "名稱最多 100 字元")]
    public string Name { get; set; } = string.Empty;

    /// <summary>API Key 描述</summary>
    [MaxLength(500, ErrorMessage = "描述最多 500 字元")]
    public string? Description { get; set; }

    /// <summary>啟用狀態</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>新的過期時間（可為空，表示永不過期）</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>權限設定 - 允許的訊息類型 ID 清單</summary>
    public List<int>? AllowedMessageTypeIds { get; set; }

    /// <summary>權限設定 - 允許的群組 ID 清單</summary>
    public List<int>? AllowedGroupIds { get; set; }
}
