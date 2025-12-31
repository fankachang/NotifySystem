using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Controllers;

/// <summary>
/// Admin 群組管理控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/groups")]
[Authorize(Policy = "AdminOnly")]
public class AdminGroupsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminGroupsController> _logger;

    public AdminGroupsController(AppDbContext dbContext, ILogger<AdminGroupsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 取得群組列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AdminGroupResponse>>), 200)]
    public async Task<IActionResult> GetGroups([FromQuery] string? search = null)
    {
        var query = _dbContext.Groups
            .Include(g => g.Members)
            .Include(g => g.MessageTypes)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(g => g.Name.Contains(search) || g.Code.Contains(search));
        }

        var groups = await query
            .OrderBy(g => g.Name)
            .Select(g => new AdminGroupResponse
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                Description = g.Description,
                IsActive = g.IsActive,
                MemberCount = g.Members.Count,
                MessageTypeCount = g.MessageTypes.Count,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AdminGroupResponse>>.Ok(groups));
    }

    /// <summary>
    /// 取得群組詳情
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AdminGroupDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetGroup(int id)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.MessageTypes)
                .ThenInclude(gmt => gmt.MessageType)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        return Ok(ApiResponse<AdminGroupDetailResponse>.Ok(new AdminGroupDetailResponse
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new GroupMemberItem
            {
                UserId = m.UserId,
                UserName = m.User?.DisplayName ?? string.Empty,
                LineUserId = m.User?.LineUserId,
                JoinedAt = m.JoinedAt
            }).ToList(),
            MessageTypes = group.MessageTypes.Select(gmt => new GroupMessageTypeItem
            {
                MessageTypeId = gmt.MessageTypeId,
                Code = gmt.MessageType.Code,
                Name = gmt.MessageType.Name,
                Color = gmt.MessageType.Color
            }).ToList()
        }));
    }

    /// <summary>
    /// 建立群組
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AdminGroupResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> CreateGroup([FromBody] AdminCreateGroupRequest request)
    {
        // 檢查代碼是否已存在
        var existingGroup = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Code == request.Code);
        if (existingGroup != null)
        {
            throw new ConflictException(ErrorCodes.DUPLICATE_ENTRY, $"群組代碼 '{request.Code}' 已存在");
        }

        var group = new Group
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員建立群組: {GroupCode}", request.Code);

        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, ApiResponse<AdminGroupResponse>.Ok(new AdminGroupResponse
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = 0,
            MessageTypeCount = 0,
            CreatedAt = group.CreatedAt
        }));
    }

    /// <summary>
    /// 更新群組
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AdminGroupResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] AdminUpdateGroupRequest request)
    {
        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        // 檢查代碼是否已被其他群組使用
        if (!string.IsNullOrEmpty(request.Code))
        {
            var existingGroup = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Code == request.Code && g.Id != id);
            if (existingGroup != null)
            {
                throw new ConflictException(ErrorCodes.DUPLICATE_ENTRY, $"群組代碼 '{request.Code}' 已被使用");
            }
            group.Code = request.Code;
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            group.Name = request.Name;
        }

        group.Description = request.Description ?? group.Description;
        group.IsActive = request.IsActive ?? group.IsActive;
        group.HostFilter = request.HostFilter ?? group.HostFilter;
        group.ServiceFilter = request.ServiceFilter ?? group.ServiceFilter;
        group.AllowDuplicateAlerts = !(request.SuppressDuplicate ?? !group.AllowDuplicateAlerts);
        
        // 處理接收時段
        if (!string.IsNullOrEmpty(request.ActiveTimeStart))
        {
            group.ReceiveTimeStart = request.ActiveTimeStart;
        }
        if (!string.IsNullOrEmpty(request.ActiveTimeEnd))
        {
            group.ReceiveTimeEnd = request.ActiveTimeEnd;
        }
        
        group.UpdatedAt = DateTime.UtcNow;

        // 處理訊息類型更新
        if (request.MessageTypeIds != null)
        {
            var existingTypes = await _dbContext.GroupMessageTypes
                .Where(gmt => gmt.GroupId == id)
                .ToListAsync();
            _dbContext.GroupMessageTypes.RemoveRange(existingTypes);

            foreach (var typeId in request.MessageTypeIds)
            {
                _dbContext.GroupMessageTypes.Add(new GroupMessageType
                {
                    GroupId = id,
                    MessageTypeId = typeId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員更新群組: {GroupId}", id);

        return Ok(ApiResponse<AdminGroupResponse>.Ok(new AdminGroupResponse
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = await _dbContext.GroupMembers.CountAsync(m => m.GroupId == id),
            MessageTypeCount = await _dbContext.GroupMessageTypes.CountAsync(gmt => gmt.GroupId == id),
            CreatedAt = group.CreatedAt
        }));
    }

    /// <summary>
    /// 刪除群組
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        // 先刪除關聯的成員和訊息類型
        var members = await _dbContext.GroupMembers.Where(m => m.GroupId == id).ToListAsync();
        var messageTypes = await _dbContext.GroupMessageTypes.Where(gmt => gmt.GroupId == id).ToListAsync();

        _dbContext.GroupMembers.RemoveRange(members);
        _dbContext.GroupMessageTypes.RemoveRange(messageTypes);
        _dbContext.Groups.Remove(group);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員刪除群組: {GroupId}", id);

        return Ok(ApiResponse.Ok("群組已刪除"));
    }

    /// <summary>
    /// 新增群組成員
    /// </summary>
    [HttpPost("{id:int}/members")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest request)
    {
        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // 檢查是否已是成員
        var existingMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == request.UserId);

        if (existingMember != null)
        {
            // 已是成員，不做任何事
            return Ok(ApiResponse.Ok("已是群組成員"));
        }

        _dbContext.GroupMembers.Add(new GroupMember
        {
            GroupId = id,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員新增群組成員: GroupId={GroupId}, UserId={UserId}", id, request.UserId);

        return Ok(ApiResponse.Ok("成員已新增"));
    }

    /// <summary>
    /// 移除群組成員
    /// </summary>
    [HttpDelete("{id:int}/members/{userId:int}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var member = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

        if (member == null)
        {
            throw new NotFoundException("GroupMember", userId);
        }

        _dbContext.GroupMembers.Remove(member);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員移除群組成員: GroupId={GroupId}, UserId={UserId}", id, userId);

        return Ok(ApiResponse.Ok("成員已移除"));
    }

    /// <summary>
    /// 設定群組的訊息類型
    /// </summary>
    [HttpPut("{id:int}/message-types")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> SetMessageTypes(int id, [FromBody] SetMessageTypesRequest request)
    {
        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        // 先移除現有的
        var existing = await _dbContext.GroupMessageTypes.Where(gmt => gmt.GroupId == id).ToListAsync();
        _dbContext.GroupMessageTypes.RemoveRange(existing);

        // 新增新的
        foreach (var typeId in request.MessageTypeIds)
        {
            _dbContext.GroupMessageTypes.Add(new GroupMessageType
            {
                GroupId = id,
                MessageTypeId = typeId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員設定群組訊息類型: GroupId={GroupId}, Count={Count}", id, request.MessageTypeIds.Count);

        return Ok(ApiResponse.Ok("訊息類型已更新"));
    }
}

/// <summary>
/// Admin 群組回應
/// </summary>
public class AdminGroupResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public int MessageTypeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Admin 群組詳情回應
/// </summary>
public class AdminGroupDetailResponse : AdminGroupResponse
{
    public List<GroupMemberItem> Members { get; set; } = new();
    public List<GroupMessageTypeItem> MessageTypes { get; set; } = new();
}

/// <summary>
/// 群組成員項目
/// </summary>
public class GroupMemberItem
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? LineUserId { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// 群組訊息類型項目
/// </summary>
public class GroupMessageTypeItem
{
    public int MessageTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

/// <summary>
/// 建立群組請求
/// </summary>
public class AdminCreateGroupRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 更新群組請求
/// </summary>
public class AdminUpdateGroupRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public string? HostFilter { get; set; }
    public string? ServiceFilter { get; set; }
    public string? ActiveTimeStart { get; set; }
    public string? ActiveTimeEnd { get; set; }
    public bool? SuppressDuplicate { get; set; }
    public List<int>? MessageTypeIds { get; set; }
}

/// <summary>
/// 新增成員請求
/// </summary>
public class AddMemberRequest
{
    public int UserId { get; set; }
}

/// <summary>
/// 設定訊息類型請求
/// </summary>
public class SetMessageTypesRequest
{
    public List<int> MessageTypeIds { get; set; } = new();
}
