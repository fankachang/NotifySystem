using LineNotify.Api.Data;
using LineNotify.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.BackgroundServices;

/// <summary>
/// 失敗重試背景服務
/// 負責處理發送失敗的訊息，進行重試
/// </summary>
public class RetryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetryService> _logger;

    // 重試設定
    private const int MaxRetryCount = 3;
    private const int RetryIntervalMinutes = 5;
    private const int ProcessIntervalSeconds = 60;
    private const int BatchSize = 50;

    public RetryService(
        IServiceScopeFactory scopeFactory,
        ILogger<RetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("失敗重試背景服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFailedDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理重試訊息時發生錯誤");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("失敗重試背景服務已停止");
    }

    /// <summary>
    /// 處理需要重試的訊息
    /// </summary>
    private async Task ProcessFailedDeliveriesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IMessageDispatchService>();

        // 取得需要重試的派送記錄
        var retryThreshold = DateTime.UtcNow.AddMinutes(-RetryIntervalMinutes);

        var failedDeliveries = await dbContext.MessageDeliveries
            .Include(d => d.Message)
                .ThenInclude(m => m.MessageType)
            .Include(d => d.User)
            .Where(d => d.Status == Models.DeliveryStatus.Pending &&
                        d.RetryCount > 0 &&
                        d.RetryCount < MaxRetryCount &&
                        d.UpdatedAt <= retryThreshold)
            .OrderBy(d => d.UpdatedAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (failedDeliveries.Count == 0)
        {
            return;
        }

        _logger.LogDebug("開始處理 {Count} 筆重試訊息", failedDeliveries.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var delivery in failedDeliveries)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var result = await dispatchService.RetryDeliveryAsync(delivery.Id);

            if (result.Success)
            {
                successCount++;
                _logger.LogDebug("訊息 {MessageId} 重試成功，使用者: {UserId}",
                    delivery.MessageId, delivery.UserId);
            }
            else
            {
                failCount++;
                _logger.LogWarning("訊息 {MessageId} 重試失敗，使用者: {UserId}, 原因: {Error}",
                    delivery.MessageId, delivery.UserId, result.ErrorMessage);
            }

            // 加入延遲避免 API 限流
            await Task.Delay(100, stoppingToken);
        }

        _logger.LogInformation("重試處理完成: 成功 {SuccessCount}, 失敗 {FailCount}",
            successCount, failCount);
    }
}
