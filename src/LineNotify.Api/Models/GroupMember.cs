using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 群組成員關聯實體 - 代表使用者與群組的多對多關係
/// </summary>
public class GroupMember
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的群組 ID</summary>
    [Required]
    public int GroupId { get; set; }

    /// <summary>關聯的使用者 ID</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>加入時間</summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>加入者（管理員 ID）</summary>
    public int? AddedByAdminId { get; set; }

    // 導覽屬性
    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AddedByAdminId))]
    public virtual Admin? AddedByAdmin { get; set; }
}
