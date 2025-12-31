using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 建立群組請求
/// </summary>
public class CreateGroupRequest
{
    /// <summary>群組代碼（唯一）</summary>
    [Required(ErrorMessage = "群組代碼為必填")]
    [MaxLength(50, ErrorMessage = "群組代碼不可超過 50 字元")]
    [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "群組代碼只能包含大寫字母、數字、底線和連字號")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>群組名稱</summary>
    [Required(ErrorMessage = "群組名稱為必填")]
    [MaxLength(100, ErrorMessage = "群組名稱不可超過 100 字元")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>群組說明</summary>
    [MaxLength(500, ErrorMessage = "群組說明不可超過 500 字元")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>關聯的訊息類型 ID 列表</summary>
    [JsonPropertyName("messageTypeIds")]
    public List<int>? MessageTypeIds { get; set; }

    /// <summary>來源主機篩選（支援萬用字元）</summary>
    [MaxLength(200, ErrorMessage = "主機篩選不可超過 200 字元")]
    [JsonPropertyName("hostFilter")]
    public string? HostFilter { get; set; }

    /// <summary>來源服務篩選（支援萬用字元）</summary>
    [MaxLength(200, ErrorMessage = "服務篩選不可超過 200 字元")]
    [JsonPropertyName("serviceFilter")]
    public string? ServiceFilter { get; set; }

    /// <summary>接收時段開始時間（24小時制，如 "00:00"）</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "時間格式不正確，請使用 HH:mm 格式")]
    [JsonPropertyName("activeTimeStart")]
    public string? ActiveTimeStart { get; set; }

    /// <summary>接收時段結束時間（24小時制，如 "23:59"）</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$|^24:00$", ErrorMessage = "時間格式不正確，請使用 HH:mm 格式")]
    [JsonPropertyName("activeTimeEnd")]
    public string? ActiveTimeEnd { get; set; }

    /// <summary>靜音時段開始時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "時間格式不正確，請使用 HH:mm 格式")]
    [JsonPropertyName("muteTimeStart")]
    public string? MuteTimeStart { get; set; }

    /// <summary>靜音時段結束時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$|^24:00$", ErrorMessage = "時間格式不正確，請使用 HH:mm 格式")]
    [JsonPropertyName("muteTimeEnd")]
    public string? MuteTimeEnd { get; set; }

    /// <summary>是否抑制重複告警</summary>
    [JsonPropertyName("suppressDuplicate")]
    public bool SuppressDuplicate { get; set; } = true;

    /// <summary>重複告警間隔（分鐘）</summary>
    [Range(1, 1440, ErrorMessage = "重複告警間隔必須在 1 到 1440 分鐘之間")]
    [JsonPropertyName("duplicateIntervalMinutes")]
    public int DuplicateIntervalMinutes { get; set; } = 30;
}

/// <summary>
/// 更新群組請求
/// </summary>
public class UpdateGroupRequest
{
    /// <summary>群組名稱</summary>
    [MaxLength(100, ErrorMessage = "群組名稱不可超過 100 字元")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>群組說明</summary>
    [MaxLength(500, ErrorMessage = "群組說明不可超過 500 字元")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>關聯的訊息類型 ID 列表</summary>
    [JsonPropertyName("messageTypeIds")]
    public List<int>? MessageTypeIds { get; set; }

    /// <summary>來源主機篩選</summary>
    [MaxLength(200, ErrorMessage = "主機篩選不可超過 200 字元")]
    [JsonPropertyName("hostFilter")]
    public string? HostFilter { get; set; }

    /// <summary>來源服務篩選</summary>
    [MaxLength(200, ErrorMessage = "服務篩選不可超過 200 字元")]
    [JsonPropertyName("serviceFilter")]
    public string? ServiceFilter { get; set; }

    /// <summary>接收時段開始時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "時間格式不正確")]
    [JsonPropertyName("activeTimeStart")]
    public string? ActiveTimeStart { get; set; }

    /// <summary>接收時段結束時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$|^24:00$", ErrorMessage = "時間格式不正確")]
    [JsonPropertyName("activeTimeEnd")]
    public string? ActiveTimeEnd { get; set; }

    /// <summary>靜音時段開始時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "時間格式不正確")]
    [JsonPropertyName("muteTimeStart")]
    public string? MuteTimeStart { get; set; }

    /// <summary>靜音時段結束時間</summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$|^24:00$", ErrorMessage = "時間格式不正確")]
    [JsonPropertyName("muteTimeEnd")]
    public string? MuteTimeEnd { get; set; }

    /// <summary>是否抑制重複告警</summary>
    [JsonPropertyName("suppressDuplicate")]
    public bool? SuppressDuplicate { get; set; }

    /// <summary>重複告警間隔（分鐘）</summary>
    [Range(1, 1440, ErrorMessage = "重複告警間隔必須在 1 到 1440 分鐘之間")]
    [JsonPropertyName("duplicateIntervalMinutes")]
    public int? DuplicateIntervalMinutes { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
