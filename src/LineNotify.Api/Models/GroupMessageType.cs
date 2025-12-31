using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 群組訊息類型關聯實體 - 代表群組與訊息類型的多對多關係
/// </summary>
public class GroupMessageType
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的群組 ID</summary>
    [Required]
    public int GroupId { get; set; }

    /// <summary>關聯的訊息類型 ID</summary>
    [Required]
    public int MessageTypeId { get; set; }

    /// <summary>關聯建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey(nameof(MessageTypeId))]
    public virtual MessageType MessageType { get; set; } = null!;
}
