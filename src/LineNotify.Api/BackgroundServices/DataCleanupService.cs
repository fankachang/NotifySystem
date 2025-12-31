using LineNotify.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.BackgroundServices;

/// <summary>
/// 資料自動清理背景服務
/// 定期清理超過 90 天的歷史資料
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupService> _logger;
    private readonly DataCleanupSettings _settings;

    public DataCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DataCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = configuration.GetSection("DataCleanup").Get<DataCleanupSettings>() ?? new DataCleanupSettings();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("資料自動清理服務已停用");
            return;
        }

        _logger.LogInformation("資料自動清理服務啟動，保留天數: {Days}，執行時間: {Hour}:00",
            _settings.RetentionDays, _settings.ExecutionHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 計算下次執行時間
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(_settings.ExecutionHour);

                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogDebug("下次資料清理將在 {NextRun} 執行（{Hours} 小時後）", nextRun, delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupDataAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常結束
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料清理發生錯誤");
                // 發生錯誤後等待一小時再試
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("資料自動清理服務已停止");
    }

    /// <summary>
    /// 執行資料清理
    /// </summary>
    private async Task CleanupDataAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("開始執行資料清理...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
        var totalDeleted = 0;

        try
        {
            // 1. 清理訊息派送記錄
            if (_settings.CleanupMessageDeliveries)
            {
                var deliveriesDeleted = await CleanupMessageDeliveriesAsync(dbContext, cutoffDate, stoppingToken);
                totalDeleted += deliveriesDeleted;
                _logger.LogInformation("已清理 {Count} 筆訊息派送記錄", deliveriesDeleted);
            }

            // 2. 清理訊息（沒有關聯派送記錄的）
            if (_settings.CleanupMessages)
            {
                var messagesDeleted = await CleanupMessagesAsync(dbContext, cutoffDate, stoppingToken);
                totalDeleted += messagesDeleted;
                _logger.LogInformation("已清理 {Count} 筆訊息記錄", messagesDeleted);
            }

            // 3. 清理審計日誌
            if (_settings.CleanupAuditLogs)
            {
                var auditLogsDeleted = await CleanupAuditLogsAsync(dbContext, cutoffDate, stoppingToken);
                totalDeleted += auditLogsDeleted;
                _logger.LogInformation("已清理 {Count} 筆審計日誌", auditLogsDeleted);
            }

            // 4. 清理登入記錄
            if (_settings.CleanupLoginLogs)
            {
                var loginLogsDeleted = await CleanupLoginLogsAsync(dbContext, cutoffDate, stoppingToken);
                totalDeleted += loginLogsDeleted;
                _logger.LogInformation("已清理 {Count} 筆登入記錄", loginLogsDeleted);
            }

            _logger.LogInformation("資料清理完成，共清理 {TotalCount} 筆記錄", totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料清理過程中發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 清理訊息派送記錄
    /// </summary>
    private async Task<int> CleanupMessageDeliveriesAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken stoppingToken)
    {
        var totalDeleted = 0;
        var batchSize = _settings.BatchSize;

        while (!stoppingToken.IsCancellationRequested)
        {
            var deliveriesToDelete = await dbContext.MessageDeliveries
                .Where(d => d.CreatedAt < cutoffDate)
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            if (deliveriesToDelete.Count == 0)
                break;

            dbContext.MessageDeliveries.RemoveRange(deliveriesToDelete);
            await dbContext.SaveChangesAsync(stoppingToken);

            totalDeleted += deliveriesToDelete.Count;

            // 每批次後稍作休息，避免資料庫負載過重
            if (deliveriesToDelete.Count == batchSize)
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        return totalDeleted;
    }

    /// <summary>
    /// 清理訊息記錄
    /// </summary>
    private async Task<int> CleanupMessagesAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken stoppingToken)
    {
        var totalDeleted = 0;
        var batchSize = _settings.BatchSize;

        while (!stoppingToken.IsCancellationRequested)
        {
            // 只清理沒有關聯派送記錄的訊息
            var messagesToDelete = await dbContext.Messages
                .Where(m => m.CreatedAt < cutoffDate)
                .Where(m => !m.Deliveries.Any())
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            if (messagesToDelete.Count == 0)
                break;

            dbContext.Messages.RemoveRange(messagesToDelete);
            await dbContext.SaveChangesAsync(stoppingToken);

            totalDeleted += messagesToDelete.Count;

            if (messagesToDelete.Count == batchSize)
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        return totalDeleted;
    }

    /// <summary>
    /// 清理審計日誌
    /// </summary>
    private async Task<int> CleanupAuditLogsAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken stoppingToken)
    {
        var totalDeleted = 0;
        var batchSize = _settings.BatchSize;

        while (!stoppingToken.IsCancellationRequested)
        {
            var logsToDelete = await dbContext.AuditLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            if (logsToDelete.Count == 0)
                break;

            dbContext.AuditLogs.RemoveRange(logsToDelete);
            await dbContext.SaveChangesAsync(stoppingToken);

            totalDeleted += logsToDelete.Count;

            if (logsToDelete.Count == batchSize)
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        return totalDeleted;
    }

    /// <summary>
    /// 清理登入記錄
    /// </summary>
    private async Task<int> CleanupLoginLogsAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken stoppingToken)
    {
        var totalDeleted = 0;
        var batchSize = _settings.BatchSize;

        while (!stoppingToken.IsCancellationRequested)
        {
            var logsToDelete = await dbContext.LoginLogs
                .Where(l => l.LoginAt < cutoffDate)
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            if (logsToDelete.Count == 0)
                break;

            dbContext.LoginLogs.RemoveRange(logsToDelete);
            await dbContext.SaveChangesAsync(stoppingToken);

            totalDeleted += logsToDelete.Count;

            if (logsToDelete.Count == batchSize)
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        return totalDeleted;
    }
}

/// <summary>
/// 資料清理設定
/// </summary>
public class DataCleanupSettings
{
    /// <summary>是否啟用資料清理</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>資料保留天數</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>每日執行時間（小時，0-23）</summary>
    public int ExecutionHour { get; set; } = 3;

    /// <summary>每批次刪除筆數</summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>是否清理訊息派送記錄</summary>
    public bool CleanupMessageDeliveries { get; set; } = true;

    /// <summary>是否清理訊息記錄</summary>
    public bool CleanupMessages { get; set; } = true;

    /// <summary>是否清理審計日誌</summary>
    public bool CleanupAuditLogs { get; set; } = true;

    /// <summary>是否清理登入記錄</summary>
    public bool CleanupLoginLogs { get; set; } = true;
}
