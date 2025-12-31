using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 訊息實體 - 代表一則待發送或已發送的告警訊息
/// </summary>
public class Message
{
    [Key]
    public int Id { get; set; }

    /// <summary>關聯的訊息類型 ID</summary>
    [Required]
    public int MessageTypeId { get; set; }

    /// <summary>訊息標題</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>訊息內容</summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>來源主機名稱</summary>
    [MaxLength(200)]
    public string? SourceHost { get; set; }

    /// <summary>來源服務名稱</summary>
    [MaxLength(200)]
    public string? SourceService { get; set; }

    /// <summary>來源 IP 位址</summary>
    [MaxLength(50)]
    public string? SourceIp { get; set; }

    /// <summary>額外的 metadata（JSON 格式）</summary>
    [Column(TypeName = "json")]
    public string? Metadata { get; set; }

    /// <summary>訊息優先級</summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "normal"; // high, normal, low

    /// <summary>指定目標群組（JSON 陣列，為空表示發送給所有訂閱者）</summary>
    [Column(TypeName = "json")]
    public string? TargetGroups { get; set; }

    /// <summary>發送此訊息的 API Key ID</summary>
    public int? ApiKeyId { get; set; }

    /// <summary>訊息建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>訊息處理完成時間</summary>
    public DateTime? ProcessedAt { get; set; }

    // 導覽屬性
    [ForeignKey(nameof(MessageTypeId))]
    public virtual MessageType MessageType { get; set; } = null!;

    [ForeignKey(nameof(ApiKeyId))]
    public virtual ApiKey? ApiKey { get; set; }

    public virtual ICollection<MessageDelivery> Deliveries { get; set; } = new List<MessageDelivery>();
}
