using LineNotify.Api.Services;

namespace LineNotify.Api.BackgroundServices;

/// <summary>
/// 訊息發送背景服務
/// 負責從佇列中取出待發送的訊息並發送給使用者
/// </summary>
public class MessageSenderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageSenderService> _logger;

    // 批次處理設定
    private const int BatchSize = 50;
    private const int ProcessIntervalSeconds = 5;
    private const int MaxParallelSends = 10;

    public MessageSenderService(
        IServiceScopeFactory scopeFactory,
        ILogger<MessageSenderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("訊息發送背景服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理待發送訊息時發生錯誤");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("訊息發送背景服務已停止");
    }

    /// <summary>
    /// 處理待發送的訊息
    /// </summary>
    private async Task ProcessPendingDeliveriesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IMessageDispatchService>();
        var messagingService = scope.ServiceProvider.GetRequiredService<ILineMessagingService>();

        // 取得待發送的派送記錄
        var pendingDeliveries = await dispatchService.GetPendingDeliveriesAsync(BatchSize);

        if (pendingDeliveries.Count == 0)
        {
            return;
        }

        _logger.LogDebug("開始處理 {Count} 筆待發送訊息", pendingDeliveries.Count);

        // 按訊息分組，同一訊息可以合併發送（Multicast）
        var groupedByMessage = pendingDeliveries
            .GroupBy(d => d.MessageId)
            .ToList();

        foreach (var messageGroup in groupedByMessage)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var deliveries = messageGroup.ToList();
            var message = deliveries.First().Message;

            // 過濾出有 Line User ID 的使用者
            var validDeliveries = deliveries
                .Where(d => d.User?.LineUserId != null)
                .ToList();

            var invalidDeliveries = deliveries
                .Where(d => d.User?.LineUserId == null)
                .ToList();

            // 標記無效的派送為失敗
            foreach (var invalid in invalidDeliveries)
            {
                await dispatchService.UpdateDeliveryStatusAsync(
                    invalid.Id,
                    "failed",
                    "使用者未綁定 Line");
            }

            if (validDeliveries.Count == 0)
            {
                continue;
            }

            // 建立告警訊息
            var alertContent = new AlertMessageContent
            {
                MessageType = message.MessageType.Code,
                Title = message.Title,
                Content = message.Content,
                SourceHost = message.SourceHost,
                SourceService = message.SourceService,
                SourceIp = message.SourceIp,
                Priority = message.Priority,
                Timestamp = message.CreatedAt
            };

            // 根據使用者數量決定發送方式
            if (validDeliveries.Count == 1)
            {
                // 單一使用者：Push Message
                var delivery = validDeliveries.First();
                var result = await messagingService.SendAlertFlexMessageAsync(
                    delivery.User!.LineUserId!,
                    alertContent);

                await UpdateDeliveryResult(dispatchService, delivery.Id, result);
            }
            else
            {
                // 多使用者：Multicast（分批處理）
                var userIds = validDeliveries
                    .Select(d => d.User!.LineUserId!)
                    .ToList();

                var result = await messagingService.SendMulticastAlertFlexMessageAsync(
                    userIds,
                    alertContent);

                // 更新所有派送狀態
                foreach (var delivery in validDeliveries)
                {
                    var isSuccess = result.Success &&
                        (result.FailedUserIds == null || !result.FailedUserIds.Contains(delivery.User!.LineUserId!));

                    await dispatchService.UpdateDeliveryStatusAsync(
                        delivery.Id,
                        isSuccess ? "sent" : "pending",
                        isSuccess ? null : result.ErrorMessage);
                }
            }
        }

        _logger.LogDebug("完成處理 {Count} 筆待發送訊息", pendingDeliveries.Count);
    }

    /// <summary>
    /// 更新單一派送結果
    /// </summary>
    private static async Task UpdateDeliveryResult(
        IMessageDispatchService dispatchService,
        int deliveryId,
        LineMessageResult result)
    {
        await dispatchService.UpdateDeliveryStatusAsync(
            deliveryId,
            result.Success ? "sent" : "pending",
            result.Success ? null : result.ErrorMessage);
    }
}
