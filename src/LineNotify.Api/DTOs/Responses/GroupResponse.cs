using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 群組回應
/// </summary>
public class GroupResponse
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

    /// <summary>群組說明</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>關聯的訊息類型列表</summary>
    [JsonPropertyName("messageTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SimpleMessageTypeResponse>? MessageTypes { get; set; }

    /// <summary>來源主機篩選</summary>
    [JsonPropertyName("hostFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HostFilter { get; set; }

    /// <summary>來源服務篩選</summary>
    [JsonPropertyName("serviceFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceFilter { get; set; }

    /// <summary>接收時段開始時間</summary>
    [JsonPropertyName("activeTimeStart")]
    public string ActiveTimeStart { get; set; } = "00:00";

    /// <summary>接收時段結束時間</summary>
    [JsonPropertyName("activeTimeEnd")]
    public string ActiveTimeEnd { get; set; } = "24:00";

    /// <summary>靜音時段開始時間</summary>
    [JsonPropertyName("muteTimeStart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MuteTimeStart { get; set; }

    /// <summary>靜音時段結束時間</summary>
    [JsonPropertyName("muteTimeEnd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MuteTimeEnd { get; set; }

    /// <summary>是否抑制重複告警</summary>
    [JsonPropertyName("suppressDuplicate")]
    public bool SuppressDuplicate { get; set; }

    /// <summary>重複告警間隔（分鐘）</summary>
    [JsonPropertyName("duplicateIntervalMinutes")]
    public int DuplicateIntervalMinutes { get; set; }

    /// <summary>成員數量</summary>
    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新時間</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 群組詳細回應（含成員和訊息類型）
/// </summary>
public class GroupDetailResponse
{
    /// <summary>群組 ID</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>群組名稱</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>群組說明</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>群組成員列表</summary>
    [JsonPropertyName("members")]
    public List<GroupMemberResponse> Members { get; set; } = new();

    /// <summary>訊息類型列表</summary>
    [JsonPropertyName("messageTypes")]
    public List<MessageTypeResponse> MessageTypes { get; set; } = new();

    /// <summary>成員數量</summary>
    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新時間</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 群組列表項目回應
/// </summary>
public class GroupListItemResponse
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

    /// <summary>成員數量</summary>
    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    /// <summary>訊息類型數量</summary>
    [JsonPropertyName("messageTypeCount")]
    public int MessageTypeCount { get; set; }

    /// <summary>啟用狀態</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 群組成員回應
/// </summary>
public class GroupMemberResponse
{
    /// <summary>使用者 ID</summary>
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    /// <summary>使用者 ID（別名）</summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get => UserId; set => UserId = value; }

    /// <summary>顯示名稱</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>頭像 URL</summary>
    [JsonPropertyName("avatarUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; set; }

    /// <summary>加入時間</summary>
    [JsonPropertyName("joinedAt")]
    public DateTime JoinedAt { get; set; }
}
