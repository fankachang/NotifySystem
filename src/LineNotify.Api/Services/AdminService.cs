using System.Security.Cryptography;
using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 管理員服務實作
/// </summary>
public class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext dbContext, ILogger<AdminService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Admin?> ValidateLoginAsync(string username, string password)
    {
        var admin = await _dbContext.Admins
            .FirstOrDefaultAsync(a => a.Username == username.ToUpper());

        if (admin == null)
        {
            _logger.LogWarning("管理員登入失敗: 帳號 {Username} 不存在", username);
            return null;
        }

        if (!admin.IsActive)
        {
            _logger.LogWarning("管理員登入失敗: 帳號 {Username} 已停用", username);
            return null;
        }

        // 驗證密碼
        if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
        {
            _logger.LogWarning("管理員登入失敗: 帳號 {Username} 密碼錯誤", username);
            return null;
        }

        // 更新最後登入時間
        admin.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員 {Username} 登入成功", username);
        return admin;
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(int adminId, string currentPassword, string newPassword)
    {
        var admin = await _dbContext.Admins.FindAsync(adminId);
        if (admin == null)
            return false;

        // 驗證目前密碼
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash))
        {
            _logger.LogWarning("管理員 {AdminId} 修改密碼失敗: 目前密碼錯誤", adminId);
            return false;
        }

        // 更新密碼
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        admin.MustChangePassword = false;
        admin.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員 {AdminId} 已修改密碼", adminId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<AdminListResponse>> GetAdminsAsync()
    {
        return await _dbContext.Admins
            .Include(a => a.LinkedUser)
            .OrderBy(a => a.Username)
            .Select(a => new AdminListResponse
            {
                Id = a.Id,
                Username = a.Username,
                DisplayName = a.DisplayName,
                IsSuperAdmin = a.IsSuperAdmin,
                MustChangePassword = a.MustChangePassword,
                LinkedUser = a.LinkedUser != null ? new SimpleUserResponse
                {
                    Id = a.LinkedUser.Id,
                    DisplayName = a.LinkedUser.DisplayName
                } : null,
                CreatedAt = a.CreatedAt,
                LastLoginAt = a.LastLoginAt
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Admin?> GetAdminByIdAsync(int id)
    {
        return await _dbContext.Admins
            .Include(a => a.LinkedUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Admin> CreateAdminAsync(CreateAdminRequest request)
    {
        // 檢查帳號是否存在
        if (await IsUsernameExistsAsync(request.Username))
        {
            throw new ConflictException(ErrorCodes.ALREADY_EXISTS, $"帳號 '{request.Username}' 已存在");
        }

        var admin = new Admin
        {
            Username = request.Username.ToUpper(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            IsSuperAdmin = request.IsSuperAdmin,
            IsActive = true,
            MustChangePassword = true, // 首次登入需修改密碼
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Admins.Add(admin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員已建立: {Username}", admin.Username);

        return admin;
    }

    /// <inheritdoc/>
    public async Task<Admin> UpdateAdminAsync(int id, UpdateAdminRequest request)
    {
        var admin = await _dbContext.Admins.FindAsync(id);
        if (admin == null)
        {
            throw new NotFoundException("Admin", id);
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            admin.DisplayName = request.DisplayName;

        if (request.IsSuperAdmin.HasValue)
            admin.IsSuperAdmin = request.IsSuperAdmin.Value;

        if (request.IsActive.HasValue)
            admin.IsActive = request.IsActive.Value;

        admin.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員 {AdminId} 已更新", id);

        return admin;
    }

    /// <inheritdoc/>
    public async Task DeleteAdminAsync(int id, int currentAdminId)
    {
        if (id == currentAdminId)
        {
            throw new ConflictException(ErrorCodes.CANNOT_DELETE, "不能刪除自己的帳號");
        }

        var admin = await _dbContext.Admins.FindAsync(id);
        if (admin == null)
        {
            throw new NotFoundException("Admin", id);
        }

        // 檢查是否為最後一個超級管理員
        if (admin.IsSuperAdmin)
        {
            var superAdminCount = await _dbContext.Admins.CountAsync(a => a.IsSuperAdmin && a.IsActive);
            if (superAdminCount <= 1)
            {
                throw new ConflictException(ErrorCodes.CANNOT_DELETE, "不能刪除最後一個超級管理員");
            }
        }

        _dbContext.Admins.Remove(admin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員 {AdminId} ({Username}) 已刪除", id, admin.Username);
    }

    /// <inheritdoc/>
    public async Task<string> ResetPasswordAsync(int adminId, int currentAdminId)
    {
        var admin = await _dbContext.Admins.FindAsync(adminId);
        if (admin == null)
        {
            throw new NotFoundException("Admin", adminId);
        }

        // 產生隨機密碼
        var newPassword = GenerateRandomPassword(12);

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        admin.MustChangePassword = true;
        admin.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員 {CurrentAdminId} 重設了管理員 {AdminId} 的密碼", currentAdminId, adminId);

        return newPassword;
    }

    /// <inheritdoc/>
    public async Task<bool> IsUsernameExistsAsync(string username, int? excludeId = null)
    {
        var query = _dbContext.Admins.Where(a => a.Username == username.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #region Private Methods

    private static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }

    #endregion
}
