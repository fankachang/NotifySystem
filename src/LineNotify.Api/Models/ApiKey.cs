using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// API 金鑰實體 - 代表外部系統用於呼叫 API 的認證金鑰
/// </summary>
public class ApiKey
{
    [Key]
    public int Id { get; set; }

    /// <summary>建立此 API Key 的管理員 ID</summary>
    [Required]
    public int CreatedByAdminId { get; set; }

    /// <summary>API Key 名稱（如「Nagios Production」）</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>API Key 描述</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>金鑰雜湊（SHA-256）- 不儲存明文</summary>
    [Required]
    [MaxLength(100)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>金鑰前綴（用於顯示識別，如 "lnk_abc12..."）</summary>
    [Required]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>權限設定（JSON 格式，可限制特定訊息類型或群組）</summary>
    [Column(TypeName = "json")]
    public string? Permissions { get; set; }

    /// <summary>啟用狀態</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>過期時間（可為空，表示永不過期）</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>最後使用時間</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>使用次數統計</summary>
    public long UsageCount { get; set; } = 0;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 導覽屬性
    [ForeignKey(nameof(CreatedByAdminId))]
    public virtual Admin CreatedByAdmin { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
