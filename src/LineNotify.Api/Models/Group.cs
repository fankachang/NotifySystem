using System.ComponentModel.DataAnnotations;

namespace LineNotify.Api.Models;

/// <summary>
/// 群組實體 - 代表使用者的分類群組，是系統的核心管理單位
/// </summary>
public class Group
{
    [Key]
    public int Id { get; set; }

    /// <summary>群組代碼（唯一識別，如 INFRA、DBA、APP）</summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>群組名稱</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>群組說明</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>來源主機篩選（支援萬用字元，如 web-server-*）</summary>
    [MaxLength(200)]
    public string? HostFilter { get; set; }

    /// <summary>來源服務篩選（支援萬用字元）</summary>
    [MaxLength(200)]
    public string? ServiceFilter { get; set; }

    /// <summary>接收時段開始時間（24小時制，如 "09:00"）</summary>
    [MaxLength(10)]
    public string ReceiveTimeStart { get; set; } = "00:00";

    /// <summary>接收時段結束時間（24小時制，如 "18:00"）</summary>
    [MaxLength(10)]
    public string ReceiveTimeEnd { get; set; } = "24:00";

    /// <summary>靜音時段開始時間（可為空）</summary>
    [MaxLength(10)]
    public string? MuteTimeStart { get; set; }

    /// <summary>靜音時段結束時間（可為空）</summary>
    [MaxLength(10)]
    public string? MuteTimeEnd { get; set; }

    /// <summary>是否接收重複告警（false 表示在間隔內不重複發送）</summary>
    public bool AllowDuplicateAlerts { get; set; } = true;

    /// <summary>重複告警間隔（分鐘），預設 30 分鐘</summary>
    public int DuplicateAlertIntervalMinutes { get; set; } = 30;

    /// <summary>群組狀態：true=啟用, false=停用</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public virtual ICollection<GroupMessageType> MessageTypes { get; set; } = new List<GroupMessageType>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
