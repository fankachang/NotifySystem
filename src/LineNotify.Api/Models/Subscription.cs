using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 訂閱實體 - 代表使用者透過群組訂閱的訊息類型設定
/// 訂閱由系統自動建立（當使用者被加入群組時），使用者無法自行建立、修改或刪除訂閱
/// </summary>
public class Subscription
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的使用者 ID</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>關聯的訊息類型 ID</summary>
    [Required]
    public int MessageTypeId { get; set; }

    /// <summary>關聯的群組 ID（必填）- 訂閱必須透過群組建立</summary>
    [Required]
    public int GroupId { get; set; }

    /// <summary>訂閱狀態：true=啟用, false=停用</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(MessageTypeId))]
    public virtual MessageType MessageType { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;
}
