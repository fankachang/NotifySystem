using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Requests;

/// <summary>
/// 群組成員請求（用於新增和移除）
/// </summary>
public class GroupMembersRequest
{
    /// <summary>使用者 ID 列表</summary>
    [Required(ErrorMessage = "使用者 ID 列表為必填")]
    [MinLength(1, ErrorMessage = "至少需要一個使用者 ID")]
    [JsonPropertyName("userIds")]
    public List<int> UserIds { get; set; } = new();
}

/// <summary>
/// 批次加入群組成員請求
/// </summary>
public class AddGroupMembersRequest
{
    /// <summary>使用者 ID 列表</summary>
    [Required(ErrorMessage = "使用者 ID 列表為必填")]
    [MinLength(1, ErrorMessage = "至少需要一個使用者 ID")]
    [JsonPropertyName("userIds")]
    public List<int> UserIds { get; set; } = new();
}

/// <summary>
/// 批次移除群組成員請求
/// </summary>
public class RemoveGroupMembersRequest
{
    /// <summary>使用者 ID 列表</summary>
    [Required(ErrorMessage = "使用者 ID 列表為必填")]
    [MinLength(1, ErrorMessage = "至少需要一個使用者 ID")]
    [JsonPropertyName("userIds")]
    public List<int> UserIds { get; set; } = new();
}

/// <summary>
/// 群組訊息類型設定請求
/// </summary>
public class GroupMessageTypesRequest
{
    /// <summary>訊息類型 ID 列表</summary>
    [Required(ErrorMessage = "訊息類型 ID 列表為必填")]
    [JsonPropertyName("messageTypeIds")]
    public List<int> MessageTypeIds { get; set; } = new();
}
