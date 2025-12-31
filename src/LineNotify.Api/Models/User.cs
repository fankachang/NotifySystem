using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LineNotify.Api.Models;

/// <summary>
/// 使用者實體 - 代表透過 Line Login 註冊的使用者
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    /// <summary>Line User ID（唯一識別）</summary>
    [Required]
    [MaxLength(50)]
    public string LineUserId { get; set; } = string.Empty;

    /// <summary>顯示名稱</summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>頭像 URL</summary>
    [MaxLength(500)]
    public string? PictureUrl { get; set; }

    /// <summary>Email（可選）</summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>帳號狀態：true=啟用, false=停用</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>是否為管理員</summary>
    public bool IsAdmin { get; set; } = false;

    /// <summary>Line 存取權杖（用於後續 API 呼叫）</summary>
    [MaxLength(500)]
    public string? LineAccessToken { get; set; }

    /// <summary>Line 存取權杖到期時間</summary>
    public DateTime? LineAccessTokenExpiresAt { get; set; }

    /// <summary>註冊時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後登入時間</summary>
    public DateTime? LastLoginAt { get; set; }

    // 導覽屬性
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    public virtual ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
