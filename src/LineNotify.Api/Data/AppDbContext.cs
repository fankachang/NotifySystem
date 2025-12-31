using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Data;

/// <summary>
/// 應用程式資料庫上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // 主要實體
    public DbSet<User> Users => Set<User>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<MessageType> MessageTypes => Set<MessageType>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // 關聯實體
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMessageType> GroupMessageTypes => Set<GroupMessageType>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MessageDelivery> MessageDeliveries => Set<MessageDelivery>();

    // 日誌實體
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.LineUserId).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        // Admin 配置
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.LinkedUser)
                  .WithMany()
                  .HasForeignKey(e => e.LinkedUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Group 配置
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // GroupMember 配置 - 確保同一使用者不會重複加入同一群組
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Group)
                  .WithMany(g => g.Members)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.GroupMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MessageType 配置
        modelBuilder.Entity<MessageType>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // GroupMessageType 配置 - 確保同一群組不會重複關聯同一訊息類型
        modelBuilder.Entity<GroupMessageType>(entity =>
        {
            entity.HasIndex(e => new { e.GroupId, e.MessageTypeId }).IsUnique();
            entity.HasOne(e => e.Group)
                  .WithMany(g => g.MessageTypes)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MessageType)
                  .WithMany(mt => mt.GroupMessageTypes)
                  .HasForeignKey(e => e.MessageTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription 配置 - 確保同一使用者在同一群組中不會重複訂閱同一訊息類型
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.MessageTypeId, e.GroupId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Subscriptions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MessageType)
                  .WithMany(mt => mt.Subscriptions)
                  .HasForeignKey(e => e.MessageTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Group)
                  .WithMany(g => g.Subscriptions)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Message 配置
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.SourceHost, e.SourceService, e.MessageTypeId });
            entity.HasOne(e => e.MessageType)
                  .WithMany(mt => mt.Messages)
                  .HasForeignKey(e => e.MessageTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // MessageDelivery 配置
        modelBuilder.Entity<MessageDelivery>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryAt);
            entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Message)
                  .WithMany(m => m.Deliveries)
                  .HasForeignKey(e => e.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.MessageDeliveries)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiKey 配置
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.KeyPrefix);
        });

        // LoginLog 配置
        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.HasIndex(e => e.LoginAt);
            entity.HasIndex(e => new { e.UserId, e.LoginAt });
        });

        // AuditLog 配置
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // 種子資料 - 預設訊息類型
        modelBuilder.Entity<MessageType>().HasData(
            new MessageType { Id = 1, Code = "CRITICAL", Name = "嚴重", Description = "嚴重告警，需要立即處理", Priority = 1, Color = "#FF0000", Icon = "🔴", IsSystemDefault = true },
            new MessageType { Id = 2, Code = "WARNING", Name = "警告", Description = "警告告警，需要關注", Priority = 2, Color = "#FFA500", Icon = "🟠", IsSystemDefault = true },
            new MessageType { Id = 3, Code = "UNKNOWN", Name = "未知", Description = "未知狀態", Priority = 3, Color = "#808080", Icon = "⚪", IsSystemDefault = true },
            new MessageType { Id = 4, Code = "OK", Name = "正常", Description = "恢復正常", Priority = 4, Color = "#00FF00", Icon = "🟢", IsSystemDefault = true },
            new MessageType { Id = 5, Code = "INFO", Name = "資訊", Description = "一般資訊通知", Priority = 5, Color = "#0000FF", Icon = "🔵", IsSystemDefault = true }
        );

        // 種子資料 - 預設管理員帳號（密碼：Admin@2025!，BCrypt 雜湊）
        // 注意：此雜湊值對應密碼 "Admin@2025!"
        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                Id = 1,
                Username = "ADMIN",
                // BCrypt hash for "Admin@2025!"
                PasswordHash = "$2a$11$R7uyScGGvDyZob6DS5T6tO.Z2eDSAOaFva//NQ86dkq4GfDyCI7UW",
                DisplayName = "系統管理員",
                IsSuperAdmin = true,
                IsActive = true,
                MustChangePassword = true
            }
        );
    }
}
