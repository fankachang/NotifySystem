using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 建立訊息類型請求
/// </summary>
public class CreateMessageTypeRequest
{
    /// <summary>類型代碼（唯一）</summary>
    [Required(ErrorMessage = "類型代碼為必填")]
    [MaxLength(50, ErrorMessage = "類型代碼不可超過 50 字元")]
    [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "類型代碼只能包含大寫字母、數字、底線和連字號")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>類型名稱</summary>
    [Required(ErrorMessage = "類型名稱為必填")]
    [MaxLength(100, ErrorMessage = "類型名稱不可超過 100 字元")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>類型說明</summary>
    [MaxLength(500, ErrorMessage = "類型說明不可超過 500 字元")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>優先級（1-5，1 最高）</summary>
    [Range(1, 5, ErrorMessage = "優先級必須在 1 到 5 之間")]
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 3;

    /// <summary>顯示顏色（十六進位）</summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "顏色格式不正確，請使用 #RRGGBB 格式")]
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#808080";

    /// <summary>圖示名稱或 Emoji</summary>
    [MaxLength(50, ErrorMessage = "圖示不可超過 50 字元")]
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

/// <summary>
/// 更新訊息類型請求
/// </summary>
public class UpdateMessageTypeRequest
{
    /// <summary>類型名稱</summary>
    [MaxLength(100, ErrorMessage = "類型名稱不可超過 100 字元")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>類型說明</summary>
    [MaxLength(500, ErrorMessage = "類型說明不可超過 500 字元")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>優先級</summary>
    [Range(1, 5, ErrorMessage = "優先級必須在 1 到 5 之間")]
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>顯示顏色</summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "顏色格式不正確")]
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>圖示</summary>
    [MaxLength(50, ErrorMessage = "圖示不可超過 50 字元")]
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
