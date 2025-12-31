using System.Text.RegularExpressions;
using LineNotify.Api.Data;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 訂閱同步服務實作
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext dbContext, ILogger<SubscriptionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SyncUserSubscriptionsAsync(int userId, int groupId)
    {
        // 取得群組關聯的訊息類型
        var groupMessageTypeIds = await _dbContext.GroupMessageTypes
            .Where(gmt => gmt.GroupId == groupId)
            .Select(gmt => gmt.MessageTypeId)
            .ToListAsync();

        if (groupMessageTypeIds.Count == 0)
            return;

        // 取得使用者在此群組已有的訂閱
        var existingSubscriptions = await _dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.GroupId == groupId)
            .Select(s => s.MessageTypeId)
            .ToHashSetAsync();

        // 建立缺少的訂閱
        var newSubscriptions = groupMessageTypeIds
            .Where(mtId => !existingSubscriptions.Contains(mtId))
            .Select(mtId => new Subscription
            {
                UserId = userId,
                GroupId = groupId,
                MessageTypeId = mtId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _dbContext.Subscriptions.AddRange(newSubscriptions);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("已同步使用者 {UserId} 在群組 {GroupId} 的訂閱", userId, groupId);
    }

    /// <inheritdoc/>
    public async Task SyncGroupSubscriptionsAsync(int groupId)
    {
        // 取得群組所有成員
        var memberUserIds = await _dbContext.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Select(gm => gm.UserId)
            .ToListAsync();

        // 取得群組關聯的訊息類型
        var groupMessageTypeIds = await _dbContext.GroupMessageTypes
            .Where(gmt => gmt.GroupId == groupId)
            .Select(gmt => gmt.MessageTypeId)
            .ToListAsync();

        // 取得現有訂閱
        var existingSubscriptions = await _dbContext.Subscriptions
            .Where(s => s.GroupId == groupId)
            .ToListAsync();

        // 移除不在群組訊息類型中的訂閱
        var subscriptionsToRemove = existingSubscriptions
            .Where(s => !groupMessageTypeIds.Contains(s.MessageTypeId))
            .ToList();

        _dbContext.Subscriptions.RemoveRange(subscriptionsToRemove);

        // 為每個成員建立缺少的訂閱
        foreach (var userId in memberUserIds)
        {
            var userExistingMtIds = existingSubscriptions
                .Where(s => s.UserId == userId)
                .Select(s => s.MessageTypeId)
                .ToHashSet();

            var newSubscriptions = groupMessageTypeIds
                .Where(mtId => !userExistingMtIds.Contains(mtId))
                .Select(mtId => new Subscription
                {
                    UserId = userId,
                    GroupId = groupId,
                    MessageTypeId = mtId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            _dbContext.Subscriptions.AddRange(newSubscriptions);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("已同步群組 {GroupId} 所有成員的訂閱", groupId);
    }

    /// <inheritdoc/>
    public async Task RemoveUserGroupSubscriptionsAsync(int userId, int groupId)
    {
        var subscriptionsToRemove = await _dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.GroupId == groupId)
            .ToListAsync();

        _dbContext.Subscriptions.RemoveRange(subscriptionsToRemove);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("已移除使用者 {UserId} 在群組 {GroupId} 的所有訂閱", userId, groupId);
    }

    /// <inheritdoc/>
    public async Task<List<Subscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await _dbContext.Subscriptions
            .Include(s => s.MessageType)
            .Include(s => s.Group)
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<int>> GetSubscribersAsync(int messageTypeId, string? sourceHost = null, string? sourceService = null)
    {
        // 取得所有啟用的群組中，訂閱此訊息類型的使用者
        var query = _dbContext.Subscriptions
            .Include(s => s.Group)
            .Include(s => s.User)
            .Where(s => s.MessageTypeId == messageTypeId &&
                       s.IsActive &&
                       s.Group.IsActive &&
                       s.User.IsActive);

        var subscriptions = await query.ToListAsync();

        // 篩選符合來源條件的訂閱
        var filteredSubscribers = subscriptions
            .Where(s => MatchesFilter(sourceHost, s.Group.HostFilter) &&
                       MatchesFilter(sourceService, s.Group.ServiceFilter))
            .Select(s => s.UserId)
            .Distinct()
            .ToList();

        return filteredSubscribers;
    }

    #region Private Methods

    /// <summary>
    /// 檢查值是否符合篩選條件（支援萬用字元）
    /// </summary>
    private static bool MatchesFilter(string? value, string? filter)
    {
        // 無篩選條件，全部符合
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        // 無值，不符合
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // 將萬用字元轉換為正規表示式
        var pattern = "^" + Regex.Escape(filter)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);
    }

    #endregion
}
