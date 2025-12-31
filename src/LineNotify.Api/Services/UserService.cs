using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 使用者管理服務實作
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        int? groupId = null,
        bool? isActive = null)
    {
        var query = _dbContext.Users
            .Include(u => u.GroupMemberships)
                .ThenInclude(gm => gm.Group)
            .AsQueryable();

        // 搜尋篩選
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        // 群組篩選
        if (groupId.HasValue)
        {
            query = query.Where(u =>
                u.GroupMemberships.Any(gm => gm.GroupId == groupId.Value));
        }

        // 狀態篩選
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // 計算總數
        var totalItems = await query.CountAsync();

        // 分頁查詢
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 轉換為回應 DTO
        var items = users.Select(u => new UserListItem
        {
            Id = u.Id,
            DisplayName = u.DisplayName,
            AvatarUrl = u.PictureUrl,
            Email = u.Email,
            IsActive = u.IsActive,
            IsAdmin = u.IsAdmin,
            LineBindingValid = !string.IsNullOrEmpty(u.LineAccessToken) && 
                               (u.LineAccessTokenExpiresAt == null || u.LineAccessTokenExpiresAt > DateTime.UtcNow),
            Groups = u.GroupMemberships
                .Where(gm => gm.Group != null)
                .Select(gm => new SimpleGroupResponse
                {
                    Id = gm.Group!.Id,
                    Code = gm.Group.Code,
                    Name = gm.Group.Name
                }).ToList(),
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new UserListResponse
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
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _dbContext.Users
            .Include(u => u.GroupMemberships)
                .ThenInclude(gm => gm.Group)
            .Include(u => u.Subscriptions)
                .ThenInclude(s => s.MessageType)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc/>
    public async Task<User?> UpdateUserAsync(int id, string? displayName, bool? isActive, bool? isAdmin)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            user.DisplayName = displayName;
        }

        if (isActive.HasValue)
        {
            user.IsActive = isActive.Value;
        }

        if (isAdmin.HasValue)
        {
            user.IsAdmin = isAdmin.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("使用者 {UserId} 已更新", id);

        return user;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("使用者 {UserId} 已停用", id);

        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetUserCountAsync()
    {
        return await _dbContext.Users.CountAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetActiveUserCountAsync()
    {
        return await _dbContext.Users
            .Where(u => u.IsActive && u.GroupMemberships.Any())
            .CountAsync();
    }
}
