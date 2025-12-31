using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 訊息發送狀態列舉
/// </summary>
public enum DeliveryStatus
{
    /// <summary>待發送</summary>
    Pending,
    /// <summary>已發送</summary>
    Sent,
    /// <summary>發送失敗</summary>
    Failed,
    /// <summary>已跳過（如重複告警被抑制）</summary>
    Skipped
}

/// <summary>
/// 訊息發送記錄實體 - 代表一則訊息對特定使用者的發送記錄
/// </summary>
public class MessageDelivery
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的訊息 ID</summary>
    [Required]
    public int MessageId { get; set; }

    /// <summary>關聯的使用者 ID</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>發送狀態</summary>
    [Required]
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    /// <summary>Line 回傳的訊息 ID</summary>
    [MaxLength(100)]
    public string? LineMessageId { get; set; }

    /// <summary>排程發送時間</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>實際發送時間</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>錯誤訊息（發送失敗時記錄）</summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>重試次數</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>下次重試時間</summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>跳過原因（Status 為 Skipped 時記錄）</summary>
    [MaxLength(500)]
    public string? SkipReason { get; set; }

    /// <summary>發送記錄建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    [ForeignKey(nameof(MessageId))]
    public virtual Message Message { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
