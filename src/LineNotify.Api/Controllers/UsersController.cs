using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 使用者管理控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 取得使用者列表
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/users?page=1&amp;pageSize=20&amp;search=王&amp;groupId=1&amp;isActive=true
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserListResponse>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? groupId = null,
        [FromQuery] bool? isActive = null)
    {
        // 限制每頁最大筆數
        pageSize = Math.Min(pageSize, 100);

        var result = await _userService.GetUsersAsync(page, pageSize, search, groupId, isActive);

        return Ok(ApiResponse<UserListResponse>.Ok(result));
    }

    /// <summary>
    /// 取得單一使用者
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/users/{id}
    /// </remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserListItem>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        var response = new UserListItem
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.PictureUrl,
            Email = user.Email,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            LineBindingValid = !string.IsNullOrEmpty(user.LineAccessToken) && 
                               (user.LineAccessTokenExpiresAt == null || user.LineAccessTokenExpiresAt > DateTime.UtcNow),
            Groups = user.GroupMemberships
                .Where(gm => gm.Group != null)
                .Select(gm => new SimpleGroupResponse
                {
                    Id = gm.Group!.Id,
                    Code = gm.Group.Code,
                    Name = gm.Group.Name
                }).ToList(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserListItem>.Ok(response));
    }

    /// <summary>
    /// 更新使用者
    /// </summary>
    /// <remarks>
    /// PATCH /api/v1/admin/users/{id}
    /// </remarks>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserListItem>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request.DisplayName, request.IsActive, request.IsAdmin);

        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        // 重新載入使用者資訊
        var updatedUser = await _userService.GetUserByIdAsync(id);

        var response = new UserListItem
        {
            Id = updatedUser!.Id,
            DisplayName = updatedUser.DisplayName,
            AvatarUrl = updatedUser.PictureUrl,
            Email = updatedUser.Email,
            IsActive = updatedUser.IsActive,
            IsAdmin = updatedUser.IsAdmin,
            LineBindingValid = !string.IsNullOrEmpty(updatedUser.LineAccessToken) && 
                               (updatedUser.LineAccessTokenExpiresAt == null || updatedUser.LineAccessTokenExpiresAt > DateTime.UtcNow),
            Groups = updatedUser.GroupMemberships
                .Where(gm => gm.Group != null)
                .Select(gm => new SimpleGroupResponse
                {
                    Id = gm.Group!.Id,
                    Code = gm.Group.Code,
                    Name = gm.Group.Name
                }).ToList(),
            CreatedAt = updatedUser.CreatedAt,
            LastLoginAt = updatedUser.LastLoginAt
        };

        return Ok(ApiResponse<UserListItem>.Ok(response));
    }
}
