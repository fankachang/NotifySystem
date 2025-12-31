using System.ComponentModel.DataAnnotations;

namespace LineNotify.Api.Models;

/// <summary>
/// 管理員實體 - 代表可以登入後台的管理員帳號
/// </summary>
public class Admin
{
    [Key]
    public int Id { get; set; }

    /// <summary>帳號名稱（登入用）</summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>密碼雜湊（BCrypt）</summary>
    [Required]
    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>顯示名稱</summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>是否為超級管理員</summary>
    public bool IsSuperAdmin { get; set; } = false;

    /// <summary>帳號狀態：true=啟用, false=停用</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>是否需強制修改密碼（首次登入時設為 true）</summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>關聯的 Line 使用者 ID（可選，用於管理員也透過 Line 登入的情況）</summary>
    public int? LinkedUserId { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後更新時間</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最後登入時間</summary>
    public DateTime? LastLoginAt { get; set; }

    // 導覽屬性
    public virtual User? LinkedUser { get; set; }
}
