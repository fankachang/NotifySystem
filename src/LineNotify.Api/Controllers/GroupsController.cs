using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 群組管理 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/groups")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IGroupService groupService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有群組列表
    /// </summary>
    /// <returns>群組列表</returns>
    /// <response code="200">成功取得群組列表</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GroupResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups()
    {
        var groups = await _groupService.GetAllGroupsAsync();
        return Ok(ApiResponse<List<GroupResponse>>.Ok(groups, "群組列表取得成功"));
    }

    /// <summary>
    /// 取得指定群組詳細資訊
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <returns>群組詳細資訊（含成員和訊息類型）</returns>
    /// <response code="200">成功取得群組詳細資訊</response>
    /// <response code="404">群組不存在</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<GroupDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(int id)
    {
        var group = await _groupService.GetGroupEntityByIdAsync(id);
        if (group == null)
        {
            throw new NotFoundException("Group", id);
        }

        var response = new GroupDetailResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            Members = group.Members.Select(m => new GroupMemberResponse
            {
                UserId = m.UserId,
                DisplayName = m.User?.DisplayName ?? "",
                JoinedAt = m.JoinedAt
            }).ToList(),
            MessageTypes = group.MessageTypes.Select(gmt => new MessageTypeResponse
            {
                Id = gmt.MessageType.Id,
                Code = gmt.MessageType.Code,
                Name = gmt.MessageType.Name,
                Description = gmt.MessageType.Description,
                IsActive = gmt.MessageType.IsActive,
                CreatedAt = gmt.MessageType.CreatedAt
            }).ToList(),
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };

        return Ok(ApiResponse<GroupDetailResponse>.Ok(response));
    }

    /// <summary>
    /// 建立新群組
    /// </summary>
    /// <param name="request">群組建立請求</param>
    /// <returns>新建立的群組資訊</returns>
    /// <response code="201">群組建立成功</response>
    /// <response code="400">請求格式錯誤</response>
    /// <response code="409">群組名稱已存在</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var group = await _groupService.CreateGroupAsync(request);

        var response = new GroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = 0,
            CreatedAt = group.CreatedAt
        };

        _logger.LogInformation("管理員建立了新群組: {GroupName}", group.Name);

        return CreatedAtAction(
            nameof(GetGroup),
            new { id = group.Id },
            ApiResponse<GroupResponse>.Ok(response, "群組建立成功"));
    }

    /// <summary>
    /// 更新群組資訊
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <param name="request">群組更新請求</param>
    /// <returns>更新後的群組資訊</returns>
    /// <response code="200">群組更新成功</response>
    /// <response code="404">群組不存在</response>
    /// <response code="409">群組名稱已被使用</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest request)
    {
        var group = await _groupService.UpdateGroupAsync(id, request);

        var response = new GroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt
        };

        _logger.LogInformation("管理員更新了群組: {GroupId}", id);

        return Ok(ApiResponse<GroupResponse>.Ok(response, "群組更新成功"));
    }

    /// <summary>
    /// 刪除群組
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <returns>無內容</returns>
    /// <response code="204">群組刪除成功</response>
    /// <response code="404">群組不存在</response>
    /// <response code="409">群組無法刪除（有相關訂閱）</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        await _groupService.DeleteGroupAsync(id);
        
        _logger.LogInformation("管理員刪除了群組: {GroupId}", id);

        return NoContent();
    }

    /// <summary>
    /// 新增群組成員
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <param name="request">成員列表請求</param>
    /// <returns>新增的成員數量</returns>
    /// <response code="200">成員新增成功</response>
    /// <response code="404">群組不存在</response>
    [HttpPost("{id:int}/members")]
    [ProducesResponseType(typeof(ApiResponse<AddMembersResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMembers(int id, [FromBody] GroupMembersRequest request)
    {
        var addedCount = await _groupService.AddGroupMembersAsync(id, request.UserIds);

        var result = new AddMembersResult
        {
            RequestedCount = request.UserIds.Count,
            AddedCount = addedCount,
            Message = addedCount == request.UserIds.Count
                ? "所有成員已新增"
                : $"已新增 {addedCount} 位成員（部分可能已存在）"
        };

        _logger.LogInformation("群組 {GroupId} 新增了 {Count} 位成員", id, addedCount);

        return Ok(ApiResponse<AddMembersResult>.Ok(result, "成員新增完成"));
    }

    /// <summary>
    /// 移除群組成員
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <param name="request">成員列表請求</param>
    /// <returns>移除的成員數量</returns>
    /// <response code="200">成員移除成功</response>
    /// <response code="404">群組不存在</response>
    [HttpDelete("{id:int}/members")]
    [ProducesResponseType(typeof(ApiResponse<RemoveMembersResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMembers(int id, [FromBody] GroupMembersRequest request)
    {
        var removedCount = await _groupService.RemoveGroupMembersAsync(id, request.UserIds);

        var result = new RemoveMembersResult
        {
            RequestedCount = request.UserIds.Count,
            RemovedCount = removedCount,
            Message = removedCount == request.UserIds.Count
                ? "所有成員已移除"
                : $"已移除 {removedCount} 位成員（部分可能不存在）"
        };

        _logger.LogInformation("群組 {GroupId} 移除了 {Count} 位成員", id, removedCount);

        return Ok(ApiResponse<RemoveMembersResult>.Ok(result, "成員移除完成"));
    }

    /// <summary>
    /// 設定群組的訊息類型
    /// </summary>
    /// <param name="id">群組 ID</param>
    /// <param name="request">訊息類型列表</param>
    /// <returns>設定結果</returns>
    /// <response code="200">設定成功</response>
    /// <response code="404">群組不存在</response>
    [HttpPut("{id:int}/message-types")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMessageTypes(int id, [FromBody] GroupMessageTypesRequest request)
    {
        await _groupService.SetGroupMessageTypesAsync(id, request.MessageTypeIds);

        _logger.LogInformation("群組 {GroupId} 更新了訊息類型設定", id);

        return Ok(ApiResponse<object>.Ok(null, "訊息類型設定成功"));
    }
}

#region Result DTOs

/// <summary>
/// 新增成員結果
/// </summary>
public class AddMembersResult
{
    public int RequestedCount { get; set; }
    public int AddedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 移除成員結果
/// </summary>
public class RemoveMembersResult
{
    public int RequestedCount { get; set; }
    public int RemovedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
