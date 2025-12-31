using System.ComponentModel.DataAnnotations;

namespace LineNotify.Api.Models;

/// <summary>
/// 訊息類型實體 - 代表告警的類型分類
/// </summary>
public class MessageType
{
    [Key]
    public int Id { get; set; }

    /// <summary>類型代碼（唯一，如 CRITICAL、WARNING）</summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>類型名稱</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>類型說明</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>優先級（1 為最高，5 為最低）</summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>顯示顏色（十六進位，如 #FF0000）</summary>
    [MaxLength(10)]
    public string Color { get; set; } = "#000000";

    /// <summary>圖示名稱或 URL</summary>
    [MaxLength(200)]
    public string? Icon { get; set; }

    /// <summary>是否為系統預設類型（不可刪除）</summary>
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>啟用狀態</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    public virtual ICollection<GroupMessageType> GroupMessageTypes { get; set; } = new List<GroupMessageType>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
