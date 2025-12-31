using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 登入記錄實體 - 代表使用者的登入歷史
/// </summary>
public class LoginLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的使用者 ID（可為空，記錄失敗的登入嘗試時可能無法識別使用者）</summary>
    public int? UserId { get; set; }

    /// <summary>關聯的管理員 ID（管理員登入時記錄）</summary>
    public int? AdminId { get; set; }

    /// <summary>登入類型</summary>
    [Required]
    [MaxLength(20)]
    public string LoginType { get; set; } = "line"; // line, admin

    /// <summary>IP 位址</summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>User Agent</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>登入是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>失敗原因（登入失敗時記錄）</summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>登入時間</summary>
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(AdminId))]
    public virtual Admin? Admin { get; set; }
}
