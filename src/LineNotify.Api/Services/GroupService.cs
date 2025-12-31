using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 群組服務實作
/// </summary>
public class GroupService : IGroupService
{
    private readonly AppDbContext _dbContext;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        AppDbContext dbContext,
        ISubscriptionService subscriptionService,
        ILogger<GroupService> logger)
    {
        _dbContext = dbContext;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResponse<GroupListItemResponse>> GetGroupsAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null)
    {
        var query = _dbContext.Groups
            .Include(g => g.Members)
            .Include(g => g.MessageTypes)
            .AsQueryable();

        // 搜尋
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.Code.Contains(search) || g.Name.Contains(search));
        }

        // 狀態篩選
        if (isActive.HasValue)
        {
            query = query.Where(g => g.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(g => g.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GroupListItemResponse
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                MemberCount = g.Members.Count,
                MessageTypeCount = g.MessageTypes.Count,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<GroupListItemResponse>
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };
    }

    /// <inheritdoc/>
    public async Task<List<GroupResponse>> GetAllGroupsAsync()
    {
        return await _dbContext.Groups
            .Include(g => g.Members)
            .Include(g => g.MessageTypes)
                .ThenInclude(gmt => gmt.MessageType)
            .OrderBy(g => g.Code)
            .Select(g => new GroupResponse
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                Description = g.Description,
                MemberCount = g.Members.Count,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<GroupResponse?> GetGroupByIdAsync(int id)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
            .Include(g => g.MessageTypes)
                .ThenInclude(gmt => gmt.MessageType)
            .FirstOrDefaultAsync(g => g.Id == id);

        return group != null ? MapToResponse(group) : null;
    }

    /// <inheritdoc/>
    public async Task<Group?> GetGroupEntityByIdAsync(int id)
    {
        return await _dbContext.Groups
            .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
            .Include(g => g.MessageTypes)
                .ThenInclude(gmt => gmt.MessageType)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    /// <inheritdoc/>
    public async Task<GroupResponse?> GetGroupByCodeAsync(string code)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
            .Include(g => g.MessageTypes)
                .ThenInclude(gmt => gmt.MessageType)
            .FirstOrDefaultAsync(g => g.Code == code);

        return group != null ? MapToResponse(group) : null;
    }

    /// <inheritdoc/>
    public async Task<Group> CreateGroupAsync(CreateGroupRequest request)
    {
        // 檢查代碼是否重複
        if (await IsCodeExistsAsync(request.Code))
        {
            throw new ConflictException(ErrorCodes.ALREADY_EXISTS, $"群組代碼 '{request.Code}' 已存在");
        }

        // 建立群組
        var group = new Group
        {
            Code = request.Code.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            HostFilter = request.HostFilter,
            ServiceFilter = request.ServiceFilter,
            ReceiveTimeStart = request.ActiveTimeStart ?? "00:00",
            ReceiveTimeEnd = request.ActiveTimeEnd ?? "24:00",
            MuteTimeStart = request.MuteTimeStart,
            MuteTimeEnd = request.MuteTimeEnd,
            AllowDuplicateAlerts = !request.SuppressDuplicate,
            DuplicateAlertIntervalMinutes = request.DuplicateIntervalMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync();

        // 關聯訊息類型
        if (request.MessageTypeIds != null && request.MessageTypeIds.Count > 0)
        {
            await AddMessageTypesToGroupAsync(group.Id, request.MessageTypeIds);
        }

        _logger.LogInformation("群組已建立: {GroupCode} - {GroupName}", group.Code, group.Name);

        return group;
    }

    /// <inheritdoc/>
    public async Task<Group> UpdateGroupAsync(int id, UpdateGroupRequest request)
    {
        var group = await _dbContext.Groups
            .Include(g => g.MessageTypes)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        // 更新欄位
        if (!string.IsNullOrWhiteSpace(request.Name))
            group.Name = request.Name;

        if (request.Description != null)
            group.Description = request.Description;

        if (request.HostFilter != null)
            group.HostFilter = request.HostFilter;

        if (request.ServiceFilter != null)
            group.ServiceFilter = request.ServiceFilter;

        if (!string.IsNullOrWhiteSpace(request.ActiveTimeStart))
            group.ReceiveTimeStart = request.ActiveTimeStart;

        if (!string.IsNullOrWhiteSpace(request.ActiveTimeEnd))
            group.ReceiveTimeEnd = request.ActiveTimeEnd;

        if (request.MuteTimeStart != null)
            group.MuteTimeStart = request.MuteTimeStart;

        if (request.MuteTimeEnd != null)
            group.MuteTimeEnd = request.MuteTimeEnd;

        if (request.SuppressDuplicate.HasValue)
            group.AllowDuplicateAlerts = !request.SuppressDuplicate.Value;

        if (request.DuplicateIntervalMinutes.HasValue)
            group.DuplicateAlertIntervalMinutes = request.DuplicateIntervalMinutes.Value;

        if (request.IsActive.HasValue)
            group.IsActive = request.IsActive.Value;

        group.UpdatedAt = DateTime.UtcNow;

        // 更新訊息類型關聯
        if (request.MessageTypeIds != null)
        {
            // 移除現有關聯
            _dbContext.GroupMessageTypes.RemoveRange(group.MessageTypes);
            await _dbContext.SaveChangesAsync();

            // 建立新關聯
            if (request.MessageTypeIds.Count > 0)
            {
                await AddMessageTypesToGroupAsync(group.Id, request.MessageTypeIds);

                // 同步訂閱
                await _subscriptionService.SyncGroupSubscriptionsAsync(group.Id);
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("群組已更新: {GroupId} - {GroupCode}", group.Id, group.Code);

        return group;
    }

    /// <inheritdoc/>
    public async Task DeleteGroupAsync(int id)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        // 檢查是否有成員
        if (group.Members.Count > 0)
        {
            throw new ConflictException(ErrorCodes.CANNOT_DELETE, $"群組 '{group.Code}' 尚有 {group.Members.Count} 位成員，請先移除所有成員");
        }

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("群組已刪除: {GroupId} - {GroupCode}", group.Id, group.Code);
    }

    /// <inheritdoc/>
    public async Task<List<GroupMemberResponse>> GetGroupMembersAsync(int groupId)
    {
        return await _dbContext.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User)
            .Select(gm => new GroupMemberResponse
            {
                Id = gm.User.Id,
                DisplayName = gm.User.DisplayName,
                AvatarUrl = gm.User.PictureUrl,
                JoinedAt = gm.JoinedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> AddGroupMembersAsync(int groupId, List<int> userIds)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            throw new NotFoundException("Group", groupId);
        }

        // 取得現有成員 ID
        var existingUserIds = group.Members.Select(m => m.UserId).ToHashSet();

        // 篩選出需要新增的使用者
        var newUserIds = userIds.Where(id => !existingUserIds.Contains(id)).ToList();

        // 驗證使用者是否存在
        var validUserIds = await _dbContext.Users
            .Where(u => newUserIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        // 新增成員
        var addedCount = 0;
        foreach (var userId in validUserIds)
        {
            _dbContext.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            addedCount++;
        }

        await _dbContext.SaveChangesAsync();

        // 同步訂閱
        foreach (var userId in validUserIds)
        {
            await _subscriptionService.SyncUserSubscriptionsAsync(userId, groupId);
        }

        _logger.LogInformation("已將 {Count} 位使用者加入群組 {GroupId}", addedCount, groupId);

        return addedCount;
    }

    /// <inheritdoc/>
    public async Task<int> RemoveGroupMembersAsync(int groupId, List<int> userIds)
    {
        var membersToRemove = await _dbContext.GroupMembers
            .Where(gm => gm.GroupId == groupId && userIds.Contains(gm.UserId))
            .ToListAsync();

        if (membersToRemove.Count == 0)
            return 0;

        _dbContext.GroupMembers.RemoveRange(membersToRemove);

        // 移除相關訂閱
        var subscriptionsToRemove = await _dbContext.Subscriptions
            .Where(s => s.GroupId == groupId && userIds.Contains(s.UserId))
            .ToListAsync();

        _dbContext.Subscriptions.RemoveRange(subscriptionsToRemove);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("已將 {Count} 位使用者從群組 {GroupId} 移除", membersToRemove.Count, groupId);

        return membersToRemove.Count;
    }

    /// <inheritdoc/>
    public async Task<bool> IsCodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _dbContext.Groups.Where(g => g.Code == code.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc/>
    public async Task SetGroupMessageTypesAsync(int groupId, List<int> messageTypeIds)
    {
        var group = await _dbContext.Groups
            .Include(g => g.MessageTypes)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            throw new NotFoundException("Group", groupId);
        }

        // 移除現有關聯
        _dbContext.GroupMessageTypes.RemoveRange(group.MessageTypes);
        await _dbContext.SaveChangesAsync();

        // 建立新關聯
        if (messageTypeIds.Count > 0)
        {
            await AddMessageTypesToGroupAsync(groupId, messageTypeIds);
            
            // 同步訂閱
            await _subscriptionService.SyncGroupSubscriptionsAsync(groupId);
        }

        _logger.LogInformation("群組 {GroupId} 已更新訊息類型設定", groupId);
    }

    #region Private Methods

    private async Task AddMessageTypesToGroupAsync(int groupId, List<int> messageTypeIds)
    {
        foreach (var mtId in messageTypeIds)
        {
            var exists = await _dbContext.MessageTypes.AnyAsync(mt => mt.Id == mtId && mt.IsActive);
            if (exists)
            {
                _dbContext.GroupMessageTypes.Add(new GroupMessageType
                {
                    GroupId = groupId,
                    MessageTypeId = mtId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private static GroupResponse MapToResponse(Group group)
    {
        return new GroupResponse
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            MessageTypes = group.MessageTypes
                .Select(gmt => new SimpleMessageTypeResponse
                {
                    Id = gmt.MessageType.Id,
                    Code = gmt.MessageType.Code,
                    Name = gmt.MessageType.Name
                }).ToList(),
            HostFilter = group.HostFilter,
            ServiceFilter = group.ServiceFilter,
            ActiveTimeStart = group.ReceiveTimeStart,
            ActiveTimeEnd = group.ReceiveTimeEnd,
            MuteTimeStart = group.MuteTimeStart,
            MuteTimeEnd = group.MuteTimeEnd,
            SuppressDuplicate = !group.AllowDuplicateAlerts,
            DuplicateIntervalMinutes = group.DuplicateAlertIntervalMinutes,
            MemberCount = group.Members.Count,
            IsActive = group.IsActive,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    #endregion
}
