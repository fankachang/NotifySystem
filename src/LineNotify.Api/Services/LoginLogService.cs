using LineNotify.Api.Data;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 登入記錄服務實作
/// </summary>
public class LoginLogService : ILoginLogService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LoginLogService> _logger;

    public LoginLogService(AppDbContext dbContext, ILogger<LoginLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LogUserLoginAsync(int? userId, bool isSuccess, string? ipAddress, string? userAgent, string? failureReason = null)
    {
        var loginLog = new LoginLog
        {
            UserId = userId,
            LoginType = "line",
            IsSuccess = isSuccess,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            FailureReason = failureReason,
            LoginAt = DateTime.UtcNow
        };

        _dbContext.LoginLogs.Add(loginLog);
        await _dbContext.SaveChangesAsync();

        if (isSuccess)
        {
            _logger.LogInformation("使用者 {UserId} 從 {IpAddress} 登入成功", userId, ipAddress);
        }
        else
        {
            _logger.LogWarning("使用者登入失敗: {FailureReason} - IP: {IpAddress}", failureReason, ipAddress);
        }
    }

    /// <inheritdoc/>
    public async Task LogAdminLoginAsync(int? adminId, bool isSuccess, string? ipAddress, string? userAgent, string? failureReason = null)
    {
        var loginLog = new LoginLog
        {
            AdminId = adminId,
            LoginType = "admin",
            IsSuccess = isSuccess,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            FailureReason = failureReason,
            LoginAt = DateTime.UtcNow
        };

        _dbContext.LoginLogs.Add(loginLog);
        await _dbContext.SaveChangesAsync();

        if (isSuccess)
        {
            _logger.LogInformation("管理員 {AdminId} 從 {IpAddress} 登入成功", adminId, ipAddress);
        }
        else
        {
            _logger.LogWarning("管理員登入失敗: {FailureReason} - IP: {IpAddress}", failureReason, ipAddress);
        }
    }

    /// <inheritdoc/>
    public async Task<List<LoginLog>> GetUserLoginLogsAsync(int userId, int count = 10)
    {
        return await _dbContext.LoginLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LoginAt)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<LoginLog>> GetAdminLoginLogsAsync(int adminId, int count = 10)
    {
        return await _dbContext.LoginLogs
            .Where(l => l.AdminId == adminId)
            .OrderByDescending(l => l.LoginAt)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> CheckLoginAttemptsAsync(string ipAddress, int withinMinutes = 15, int maxAttempts = 5)
    {
        var since = DateTime.UtcNow.AddMinutes(-withinMinutes);

        var failedAttempts = await _dbContext.LoginLogs
            .CountAsync(l => l.IpAddress == ipAddress &&
                            !l.IsSuccess &&
                            l.LoginAt >= since);

        if (failedAttempts >= maxAttempts)
        {
            _logger.LogWarning("IP {IpAddress} 在 {Minutes} 分鐘內登入失敗 {Count} 次，已被暫時封鎖",
                ipAddress, withinMinutes, failedAttempts);
            return false;
        }

        return true;
    }
}
