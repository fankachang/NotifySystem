using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 報表服務實作
/// </summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext dbContext, ILogger<ReportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ReportSummaryResponse> GetSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        // 預設為最近 30 天
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-30);

        var messagesQuery = _dbContext.Messages
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end);

        var deliveriesQuery = _dbContext.MessageDeliveries
            .Where(d => d.CreatedAt >= start && d.CreatedAt <= end);

        // 總訊息數
        var totalMessages = await messagesQuery.CountAsync();

        // 總發送數與成功率
        var totalDeliveries = await deliveriesQuery.CountAsync();
        var successfulDeliveries = await deliveriesQuery
            .Where(d => d.Status == DeliveryStatus.Sent)
            .CountAsync();
        var successRate = totalDeliveries > 0
            ? Math.Round((double)successfulDeliveries / totalDeliveries * 100, 2)
            : 0;

        // 依訊息類型統計
        var byMessageType = await messagesQuery
            .GroupBy(m => m.MessageType)
            .Select(g => new MessageTypeStatItem
            {
                Code = g.Key.Code,
                Name = g.Key.Name,
                Color = g.Key.Color,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // 依日期統計
        var byDay = await messagesQuery
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dailyStats = byDay.Select(x => new DailyStatItem
        {
            Date = x.Date.ToString("yyyy-MM-dd"),
            Count = x.Count
        }).ToList();

        // 尖峰時段
        var hourlyStats = await messagesQuery
            .GroupBy(m => m.CreatedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(4)
            .Select(x => x.Hour)
            .ToListAsync();

        return new ReportSummaryResponse
        {
            TotalMessages = totalMessages,
            TotalDeliveries = totalDeliveries,
            SuccessRate = successRate,
            ByMessageType = byMessageType,
            ByDay = dailyStats,
            PeakHours = hourlyStats
        };
    }

    /// <inheritdoc/>
    public async Task<DeliveryStatsResponse> GetDeliveryStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        // 預設為最近 7 天
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-7);

        var query = _dbContext.MessageDeliveries
            .Where(d => d.CreatedAt >= start && d.CreatedAt <= end);

        var totalDeliveries = await query.CountAsync();
        var sentCount = await query.Where(d => d.Status == DeliveryStatus.Sent).CountAsync();
        var failedCount = await query.Where(d => d.Status == DeliveryStatus.Failed).CountAsync();
        var pendingCount = await query.Where(d => d.Status == DeliveryStatus.Pending).CountAsync();
        var skippedCount = await query.Where(d => d.Status == DeliveryStatus.Skipped).CountAsync();

        var avgRetryCount = totalDeliveries > 0
            ? await query.AverageAsync(d => (double)d.RetryCount)
            : 0;

        // 依狀態統計
        var byStatus = new List<StatusStatItem>
        {
            new() { Status = "sent", Count = sentCount, Percentage = totalDeliveries > 0 ? Math.Round((double)sentCount / totalDeliveries * 100, 2) : 0 },
            new() { Status = "failed", Count = failedCount, Percentage = totalDeliveries > 0 ? Math.Round((double)failedCount / totalDeliveries * 100, 2) : 0 },
            new() { Status = "pending", Count = pendingCount, Percentage = totalDeliveries > 0 ? Math.Round((double)pendingCount / totalDeliveries * 100, 2) : 0 },
            new() { Status = "skipped", Count = skippedCount, Percentage = totalDeliveries > 0 ? Math.Round((double)skippedCount / totalDeliveries * 100, 2) : 0 }
        };

        // 依小時統計
        var hourlyData = await query
            .GroupBy(d => d.CreatedAt.Hour)
            .Select(g => new HourlyStatItem { Hour = g.Key, Count = g.Count() })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        // 補全 24 小時
        var byHour = Enumerable.Range(0, 24)
            .Select(h => hourlyData.FirstOrDefault(x => x.Hour == h) ?? new HourlyStatItem { Hour = h, Count = 0 })
            .ToList();

        return new DeliveryStatsResponse
        {
            StartDate = start,
            EndDate = end,
            TotalDeliveries = totalDeliveries,
            SentCount = sentCount,
            FailedCount = failedCount,
            PendingCount = pendingCount,
            SkippedCount = skippedCount,
            AvgRetryCount = Math.Round(avgRetryCount, 2),
            ByStatus = byStatus,
            ByHour = byHour
        };
    }

    /// <inheritdoc/>
    public async Task<SystemOverview> GetSystemOverviewAsync()
    {
        var today = DateTime.UtcNow.Date;

        return new SystemOverview
        {
            TotalUsers = await _dbContext.Users.CountAsync(),
            ActiveUsers = await _dbContext.Users
                .Where(u => u.IsActive && u.GroupMemberships.Any())
                .CountAsync(),
            TotalGroups = await _dbContext.Groups.Where(g => g.IsActive).CountAsync(),
            TotalMessageTypes = await _dbContext.MessageTypes.Where(mt => mt.IsActive).CountAsync(),
            TodayMessages = await _dbContext.Messages
                .Where(m => m.CreatedAt >= today)
                .CountAsync(),
            TodayDeliveries = await _dbContext.MessageDeliveries
                .Where(d => d.CreatedAt >= today)
                .CountAsync(),
            PendingDeliveries = await _dbContext.MessageDeliveries
                .Where(d => d.Status == DeliveryStatus.Pending)
                .CountAsync(),
            NewUsersToday = await _dbContext.Users
                .Where(u => u.CreatedAt >= today)
                .CountAsync()
        };
    }

    /// <inheritdoc/>
    public async Task<List<DailyTrendItem>> GetDailyTrendAsync(int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var dailyData = await _dbContext.Messages
            .Where(m => m.CreatedAt >= startDate)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyTrendItem { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // 補全日期
        var result = new List<DailyTrendItem>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var item = dailyData.FirstOrDefault(d => d.Date == date);
            result.Add(item ?? new DailyTrendItem { Date = date, Count = 0 });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<List<TypeDistributionItem>> GetTypeDistributionAsync(int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        return await _dbContext.Messages
            .Where(m => m.CreatedAt >= startDate)
            .GroupBy(m => m.MessageType)
            .Select(g => new TypeDistributionItem
            {
                Id = g.Key.Id,
                Code = g.Key.Code,
                Name = g.Key.Name,
                Color = g.Key.Color,
                Icon = g.Key.Icon,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<TopGroupItem>> GetTopGroupsAsync(int days, int limit)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        // 由於 Message 沒有直接的 TargetGroupId，我們使用 TargetGroups JSON 欄位
        // 暫時改用 Group 的訊息統計
        var groups = await _dbContext.Groups
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.Members.Count)
            .Take(limit)
            .Select(g => new TopGroupItem
            {
                GroupId = g.Id,
                GroupName = g.Name,
                MessageCount = _dbContext.Messages
                    .Where(m => m.CreatedAt >= startDate && m.TargetGroups != null && m.TargetGroups.Contains(g.Code))
                    .Count()
            })
            .ToListAsync();

        return groups.OrderByDescending(g => g.MessageCount).ToList();
    }

    /// <inheritdoc/>
    public async Task<SimpleDeliveryStats> GetSimpleDeliveryStatsAsync(int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var query = _dbContext.MessageDeliveries.Where(d => d.CreatedAt >= startDate);

        return new SimpleDeliveryStats
        {
            Total = await query.CountAsync(),
            Pending = await query.Where(d => d.Status == DeliveryStatus.Pending).CountAsync(),
            Sent = await query.Where(d => d.Status == DeliveryStatus.Sent).CountAsync(),
            Failed = await query.Where(d => d.Status == DeliveryStatus.Failed).CountAsync()
        };
    }
}
