using System.Text.Json;
using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 訊息派送服務實作
/// </summary>
public class MessageDispatchService : IMessageDispatchService
{
    private readonly AppDbContext _dbContext;
    private readonly ILineMessagingService _lineMessagingService;
    private readonly ILogger<MessageDispatchService> _logger;

    public MessageDispatchService(
        AppDbContext dbContext,
        ILineMessagingService lineMessagingService,
        ILogger<MessageDispatchService> logger)
    {
        _dbContext = dbContext;
        _lineMessagingService = lineMessagingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MessageDispatchResult> DispatchMessageAsync(SendMessageRequest request, int? apiKeyId = null)
    {
        // 1. 驗證訊息類型
        var messageType = await _dbContext.MessageTypes
            .FirstOrDefaultAsync(mt => mt.Code == request.MessageType && mt.IsActive);

        if (messageType == null)
        {
            throw new BadRequestException(ErrorCodes.INVALID_MESSAGE_TYPE, 
                $"訊息類型 '{request.MessageType}' 不存在或已停用");
        }

        // 2. 檢查重複訊息（抑制機制）
        if (await IsDuplicateMessageAsync(request.MessageType, request.Source?.Host, request.Source?.Service))
        {
            _logger.LogInformation("訊息被抑制（重複）: Type={Type}, Host={Host}, Service={Service}",
                request.MessageType, request.Source?.Host, request.Source?.Service);
            
            return MessageDispatchResult.Fail("DUPLICATE_MESSAGE", "訊息已被抑制（短時間內重複發送）");
        }

        // 3. 查詢訂閱者
        var subscribers = await GetSubscribersAsync(
            request.MessageType,
            request.TargetGroups,
            request.Source?.Host,
            request.Source?.Service);

        // 4. 建立訊息記錄
        var message = new Message
        {
            MessageTypeId = messageType.Id,
            Title = request.Title,
            Content = request.Content,
            SourceHost = request.Source?.Host,
            SourceService = request.Source?.Service,
            SourceIp = request.Source?.Ip,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            Priority = request.Priority,
            ApiKeyId = apiKeyId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        // 5. 建立派送記錄
        if (subscribers.Count > 0)
        {
            var deliveries = subscribers.Select(user => new MessageDelivery
            {
                MessageId = message.Id,
                UserId = user.Id,
                Status = DeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _dbContext.MessageDeliveries.AddRange(deliveries);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("訊息 {MessageId} 已建立，待發送給 {Count} 位訂閱者",
                message.Id, subscribers.Count);
        }
        else
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("訊息 {MessageId} 沒有符合條件的訂閱者", message.Id);
        }

        return MessageDispatchResult.Ok(message.Id, subscribers.Count);
    }

    /// <inheritdoc />
    public async Task<Message?> GetMessageByIdAsync(int messageId)
    {
        return await _dbContext.Messages
            .Include(m => m.MessageType)
            .Include(m => m.Deliveries)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    /// <inheritdoc />
    public async Task<MessageListResponse> GetMessagesAsync(MessageQueryRequest query)
    {
        var queryable = _dbContext.Messages
            .Include(m => m.MessageType)
            .Include(m => m.Deliveries)
            .AsQueryable();

        // 篩選條件
        if (!string.IsNullOrEmpty(query.MessageType))
        {
            queryable = queryable.Where(m => m.MessageType.Code == query.MessageType);
        }

        if (!string.IsNullOrEmpty(query.SourceHost))
        {
            queryable = queryable.Where(m => m.SourceHost != null && m.SourceHost.Contains(query.SourceHost));
        }

        if (!string.IsNullOrEmpty(query.SourceService))
        {
            queryable = queryable.Where(m => m.SourceService != null && m.SourceService.Contains(query.SourceService));
        }

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(m => m.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(m => m.CreatedAt <= query.EndDate.Value);
        }

        // 計算總數
        var totalItems = await queryable.CountAsync();

        // 分頁查詢
        var items = await queryable
            .OrderByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new MessageListItem
            {
                Id = m.Id,
                MessageType = m.MessageType.Code,
                Title = m.Title,
                SourceHost = m.SourceHost,
                RecipientCount = m.Deliveries.Count,
                SentCount = m.Deliveries.Count(d => d.Status == DeliveryStatus.Sent),
                FailedCount = m.Deliveries.Count(d => d.Status == DeliveryStatus.Failed),
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return new MessageListResponse
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems
            }
        };
    }

    /// <inheritdoc />
    public async Task<UserMessageListResponse> GetUserMessagesAsync(int userId, MessageQueryRequest query)
    {
        var queryable = _dbContext.MessageDeliveries
            .Include(d => d.Message)
                .ThenInclude(m => m.MessageType)
            .Where(d => d.UserId == userId)
            .AsQueryable();

        // 篩選條件
        if (!string.IsNullOrEmpty(query.MessageType))
        {
            queryable = queryable.Where(d => d.Message.MessageType.Code == query.MessageType);
        }

        if (!string.IsNullOrEmpty(query.SourceHost))
        {
            queryable = queryable.Where(d => d.Message.SourceHost != null && d.Message.SourceHost.Contains(query.SourceHost));
        }

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<DeliveryStatus>(query.Status, true, out var statusEnum))
        {
            queryable = queryable.Where(d => d.Status == statusEnum);
        }

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(d => d.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(d => d.CreatedAt <= query.EndDate.Value);
        }

        // 計算總數
        var totalItems = await queryable.CountAsync();

        // 分頁查詢 - 先取得資料再投影
        var deliveries = await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = deliveries.Select(d => new UserMessageItem
        {
            Id = d.Message.Id,
            MessageType = d.Message.MessageType.Code,
            Title = d.Message.Title,
            Content = d.Message.Content,
            SourceHost = d.Message.SourceHost,
            Status = d.Status.ToString().ToLowerInvariant(),
            SentAt = d.SentAt
        }).ToList();

        return new UserMessageListResponse
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems
            }
        };
    }

    /// <inheritdoc />
    public async Task<List<User>> GetSubscribersAsync(
        string messageTypeCode, 
        List<string>? targetGroups, 
        string? sourceHost, 
        string? sourceService)
    {
        // 查詢訊息類型
        var messageType = await _dbContext.MessageTypes
            .FirstOrDefaultAsync(mt => mt.Code == messageTypeCode && mt.IsActive);

        if (messageType == null)
        {
            _logger.LogWarning("找不到訊息類型: {MessageTypeCode}", messageTypeCode);
            return new List<User>();
        }

        // 查詢訂閱此訊息類型的群組
        var groupsQuery = _dbContext.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.MessageTypes)
            .Where(g => g.IsActive && g.MessageTypes.Any(gmt => gmt.MessageTypeId == messageType.Id));

        // 先取得所有符合條件的群組
        var allGroups = await groupsQuery.ToListAsync();

        // 如果指定了目標群組，則在記憶體中篩選（避免 EF Core 參數化問題）
        var groups = allGroups;
        if (targetGroups != null && targetGroups.Count > 0)
        {
            groups = allGroups.Where(g => targetGroups.Contains(g.Code)).ToList();
        }

        _logger.LogInformation("找到符合條件的群組: {GroupCount} 個, 目標群組: {TargetGroups}", 
            groups.Count, targetGroups != null ? string.Join(",", targetGroups) : "全部");

        // 篩選：根據群組的來源主機/服務篩選模式和接收時段
        var filteredGroups = groups.Where(group =>
        {
            // 檢查來源主機篩選
            if (!string.IsNullOrEmpty(group.HostFilter) && !string.IsNullOrEmpty(sourceHost))
            {
                if (!MatchWildcard(sourceHost, group.HostFilter))
                {
                    _logger.LogDebug("群組 {GroupCode} 主機篩選不符: {HostFilter} vs {SourceHost}", 
                        group.Code, group.HostFilter, sourceHost);
                    return false;
                }
            }

            // 檢查來源服務篩選
            if (!string.IsNullOrEmpty(group.ServiceFilter) && !string.IsNullOrEmpty(sourceService))
            {
                if (!MatchWildcard(sourceService, group.ServiceFilter))
                {
                    _logger.LogDebug("群組 {GroupCode} 服務篩選不符: {ServiceFilter} vs {SourceService}", 
                        group.Code, group.ServiceFilter, sourceService);
                    return false;
                }
            }

            // 檢查接收時段
            if (!IsWithinReceiveWindow(group))
            {
                _logger.LogDebug("群組 {GroupCode} 不在接收時段內", group.Code);
                return false;
            }

            return true;
        }).ToList();

        _logger.LogInformation("篩選後群組數: {FilteredCount}", filteredGroups.Count);

        // 取得所有群組的成員（使用者）
        var users = filteredGroups
            .SelectMany(g => g.Members)
            .Where(m => m.User.IsActive && !string.IsNullOrEmpty(m.User.LineAccessToken))
            .Select(m => m.User)
            .DistinctBy(u => u.Id)
            .ToList();

        _logger.LogInformation("找到符合條件的訂閱者: {UserCount} 位", users.Count);

        return users;
    }

    /// <inheritdoc />
    public async Task<bool> IsDuplicateMessageAsync(
        string messageTypeCode, 
        string? sourceHost, 
        string? sourceService, 
        int windowMinutes = 5)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var exists = await _dbContext.Messages
            .Include(m => m.MessageType)
            .AnyAsync(m =>
                m.MessageType.Code == messageTypeCode &&
                m.SourceHost == sourceHost &&
                m.SourceService == sourceService &&
                m.CreatedAt >= windowStart);

        return exists;
    }

    /// <inheritdoc />
    public async Task<LineMessageResult> RetryDeliveryAsync(int deliveryId)
    {
        var delivery = await _dbContext.MessageDeliveries
            .Include(d => d.Message)
                .ThenInclude(m => m.MessageType)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == deliveryId);

        if (delivery == null)
        {
            return LineMessageResult.Fail("NOT_FOUND", "派送記錄不存在");
        }

        if (delivery.User?.LineUserId == null)
        {
            return LineMessageResult.Fail("NO_LINE_ID", "使用者未綁定 Line");
        }

        // 建立告警訊息
        var alertContent = new AlertMessageContent
        {
            MessageType = delivery.Message.MessageType.Code,
            Title = delivery.Message.Title,
            Content = delivery.Message.Content,
            SourceHost = delivery.Message.SourceHost,
            SourceService = delivery.Message.SourceService,
            SourceIp = delivery.Message.SourceIp,
            Priority = delivery.Message.Priority,
            Timestamp = delivery.Message.CreatedAt
        };

        // 發送訊息
        var result = await _lineMessagingService.SendAlertFlexMessageAsync(
            delivery.User.LineUserId,
            alertContent);

        // 更新派送狀態
        delivery.RetryCount++;
        delivery.UpdatedAt = DateTime.UtcNow;

        if (result.Success)
        {
            delivery.Status = DeliveryStatus.Sent;
            delivery.SentAt = DateTime.UtcNow;
            delivery.ErrorMessage = null;
        }
        else
        {
            delivery.Status = delivery.RetryCount >= 3 ? DeliveryStatus.Failed : DeliveryStatus.Pending;
            delivery.ErrorMessage = result.ErrorMessage;
        }

        await _dbContext.SaveChangesAsync();

        return result;
    }

    /// <inheritdoc />
    public async Task<List<MessageDelivery>> GetPendingDeliveriesAsync(int maxCount = 100)
    {
        return await _dbContext.MessageDeliveries
            .Include(d => d.Message)
                .ThenInclude(m => m.MessageType)
            .Include(d => d.User)
            .Where(d => d.Status == DeliveryStatus.Pending && d.RetryCount < 3)
            .OrderBy(d => d.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateDeliveryStatusAsync(int deliveryId, string status, string? errorMessage = null)
    {
        var delivery = await _dbContext.MessageDeliveries.FindAsync(deliveryId);

        if (delivery != null)
        {
            if (Enum.TryParse<DeliveryStatus>(status, true, out var statusEnum))
            {
                delivery.Status = statusEnum;
            }
            delivery.ErrorMessage = errorMessage;
            delivery.UpdatedAt = DateTime.UtcNow;

            if (status.Equals("sent", StringComparison.OrdinalIgnoreCase))
            {
                delivery.SentAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            // 檢查是否所有派送都已完成
            await UpdateMessageStatusAsync(delivery.MessageId);
        }
    }

    #region Private Methods

    /// <summary>
    /// 更新訊息狀態
    /// </summary>
    private async Task UpdateMessageStatusAsync(int messageId)
    {
        var message = await _dbContext.Messages
            .Include(m => m.Deliveries)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return;

        var totalCount = message.Deliveries.Count;
        var completedCount = message.Deliveries.Count(d => d.Status == DeliveryStatus.Sent || d.Status == DeliveryStatus.Failed);

        if (completedCount == totalCount && totalCount > 0)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 萬用字元匹配
    /// </summary>
    private static bool MatchWildcard(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        if (string.IsNullOrEmpty(input)) return false;

        // 支援多個模式（逗號分隔）
        var patterns = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var p in patterns)
        {
            if (MatchSingleWildcard(input, p))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 單一萬用字元匹配
    /// </summary>
    private static bool MatchSingleWildcard(string input, string pattern)
    {
        // 簡單的萬用字元支援：* 匹配任意字元
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(
            input, 
            regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 檢查是否在接收時段內
    /// </summary>
    private static bool IsWithinReceiveWindow(Group group)
    {
        var now = DateTime.Now.TimeOfDay;

        // 解析接收時段
        if (!TimeSpan.TryParse(group.ReceiveTimeStart, out var startTime) ||
            !TimeSpan.TryParse(group.ReceiveTimeEnd, out var endTime))
        {
            return true; // 解析失敗則允許
        }

        // 檢查靜音時段
        if (!string.IsNullOrEmpty(group.MuteTimeStart) && !string.IsNullOrEmpty(group.MuteTimeEnd))
        {
            if (TimeSpan.TryParse(group.MuteTimeStart, out var muteStart) &&
                TimeSpan.TryParse(group.MuteTimeEnd, out var muteEnd))
            {
                if (muteStart <= muteEnd)
                {
                    if (now >= muteStart && now <= muteEnd)
                    {
                        return false; // 在靜音時段內
                    }
                }
                else
                {
                    // 跨午夜的靜音時段
                    if (now >= muteStart || now <= muteEnd)
                    {
                        return false;
                    }
                }
            }
        }

        // 處理 "24:00" 的情況
        if (group.ReceiveTimeEnd == "24:00")
        {
            endTime = new TimeSpan(23, 59, 59);
        }

        // 檢查接收時段
        if (startTime <= endTime)
        {
            return now >= startTime && now <= endTime;
        }
        else
        {
            // 跨午夜的接收時段
            return now >= startTime || now <= endTime;
        }
    }

    #endregion
}
