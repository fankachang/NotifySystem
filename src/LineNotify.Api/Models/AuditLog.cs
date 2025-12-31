using System.ComponentModel.DataAnnotations;

namespace LineNotify.Api.Models;

/// <summary>
/// 審計日誌實體 - 記錄所有敏感操作
/// </summary>
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>執行操作的管理員 ID（可為空，系統自動操作時無管理員）</summary>
    public int? AdminId { get; set; }

    /// <summary>執行操作的使用者 ID（可為空）</summary>
    public int? UserId { get; set; }

    /// <summary>操作類型</summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>操作的實體類型（如 User、Group、ApiKey）</summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>操作的實體 ID</summary>
    public int? EntityId { get; set; }

    /// <summary>操作前的資料（JSON 格式）</summary>
    public string? OldValues { get; set; }

    /// <summary>操作後的資料（JSON 格式）</summary>
    public string? NewValues { get; set; }

    /// <summary>IP 位址</summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>User Agent</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>操作時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    public virtual Admin? Admin { get; set; }
    public virtual User? User { get; set; }
}
