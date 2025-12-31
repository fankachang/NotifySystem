using System.Security.Claims;
using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 認證控制器 - 處理 Line Login 和管理員登入
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILineAuthService _lineAuthService;
    private readonly IJwtService _jwtService;
    private readonly ILoginLogService _loginLogService;
    private readonly IAdminService _adminService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext dbContext,
        ILineAuthService lineAuthService,
        IJwtService jwtService,
        ILoginLogService loginLogService,
        IAdminService adminService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _lineAuthService = lineAuthService;
        _jwtService = jwtService;
        _loginLogService = loginLogService;
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// 取得 Line Login 授權 URL
    /// </summary>
    /// <remarks>
    /// GET /api/v1/auth/line/login
    /// </remarks>
    [HttpGet("line/login")]
    [ProducesResponseType(typeof(ApiResponse<LineLoginUrlResponse>), 200)]
    public async Task<IActionResult> GetLineLoginUrl([FromQuery] string? redirectUri = null)
    {
        var (authUrl, state) = await _lineAuthService.GetAuthorizationUrlAsync(redirectUri);

        // 將 state 存入 Session 或 Cookie（用於後續驗證）
        // 這裡使用 Cookie 儲存
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(10)
        };
        Response.Cookies.Append("line_state", state, cookieOptions);

        return Ok(ApiResponse<LineLoginUrlResponse>.Ok(new LineLoginUrlResponse
        {
            AuthUrl = authUrl
        }));
    }

    /// <summary>
    /// 處理 Line Login 回調
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/line/callback
    /// </remarks>
    [HttpPost("line/callback")]
    [ProducesResponseType(typeof(ApiResponse<UserLoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    public async Task<IActionResult> LineLoginCallback([FromBody] LineLoginCallbackRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            // 從 Cookie 取得預期的 state
            var expectedState = Request.Cookies["line_state"];

            // 處理 Line 回調
            var (user, isNewUser) = await _lineAuthService.HandleCallbackAsync(
                request.Code,
                request.State,
                expectedState);

            // 清除 state Cookie
            Response.Cookies.Delete("line_state");

            // 載入使用者的群組和訂閱資訊
            var userWithGroups = await _dbContext.Users
                .Include(u => u.GroupMemberships)
                    .ThenInclude(gm => gm.Group)
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.MessageType)
                .FirstAsync(u => u.Id == user.Id);

            // 產生 JWT Token
            var token = _jwtService.GenerateUserToken(user.Id, user.DisplayName, user.IsAdmin);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // 記錄登入
            await _loginLogService.LogUserLoginAsync(user.Id, true, ipAddress, userAgent);

            // 組建回應
            var response = new UserLoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtService.GetExpiresInSeconds(),
                User = MapToUserResponse(userWithGroups, isNewUser)
            };

            return Ok(ApiResponse<UserLoginResponse>.Ok(response));
        }
        catch (ApiException)
        {
            await _loginLogService.LogUserLoginAsync(null, false, ipAddress, userAgent, "Line 登入失敗");
            throw;
        }
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/refresh
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // 注意：實際實作中應該將 Refresh Token 存入資料庫進行驗證
        // 這裡簡化處理，僅驗證格式
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException(ErrorCodes.INVALID_TOKEN, "Refresh Token 無效");
        }

        // TODO: 從資料庫驗證 Refresh Token 並取得關聯的使用者/管理員
        // 這裡需要另外實作 Refresh Token 的儲存和驗證機制

        // 暫時回傳錯誤，待後續實作
        throw new BadRequestException(ErrorCodes.INVALID_TOKEN, "Refresh Token 無效或已過期");
    }

    /// <summary>
    /// 登出
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/logout
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public IActionResult Logout()
    {
        // 清除相關 Cookie
        Response.Cookies.Delete("line_state");

        // TODO: 如果有實作 Token 黑名單，將當前 Token 加入黑名單

        return Ok(ApiResponse.Ok("已成功登出"));
    }

    /// <summary>
    /// 取得當前使用者資訊
    /// </summary>
    /// <remarks>
    /// GET /api/v1/auth/me
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AdminResponse>), 200)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userType = User.FindFirst("user_type")?.Value;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(subClaim, out var id))
        {
            throw new UnauthorizedException();
        }

        if (userType == "admin")
        {
            var admin = await _dbContext.Admins
                .Include(a => a.LinkedUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (admin == null)
            {
                throw new NotFoundException("Admin", id);
            }

            var response = new AdminResponse
            {
                Id = admin.Id,
                Username = admin.Username,
                DisplayName = admin.DisplayName,
                IsSuperAdmin = admin.IsSuperAdmin,
                MustChangePassword = admin.MustChangePassword,
                LinkedUser = admin.LinkedUser != null ? new SimpleUserResponse
                {
                    Id = admin.LinkedUser.Id,
                    DisplayName = admin.LinkedUser.DisplayName
                } : null,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt
            };

            return Ok(ApiResponse<AdminResponse>.Ok(response));
        }
        else
        {
            var user = await _dbContext.Users
                .Include(u => u.GroupMemberships)
                    .ThenInclude(gm => gm.Group)
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.MessageType)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new NotFoundException("User", id);
            }

            var response = new CurrentUserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = user.PictureUrl,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                Groups = user.GroupMemberships
                    .Where(gm => gm.Group.IsActive)
                    .Select(gm => new SimpleGroupResponse
                    {
                        Id = gm.Group.Id,
                        Code = gm.Group.Code,
                        Name = gm.Group.Name
                    }).ToList(),
                SubscribedMessageTypes = user.Subscriptions
                    .Where(s => s.IsActive)
                    .Select(s => s.MessageType)
                    .Distinct()
                    .Select(mt => new SimpleMessageTypeResponse
                    {
                        Code = mt.Code,
                        Name = mt.Name
                    }).ToList(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(ApiResponse<CurrentUserResponse>.Ok(response));
        }
    }

    /// <summary>
    /// 更新當前使用者資料
    /// </summary>
    /// <remarks>
    /// PATCH /api/v1/auth/me
    /// </remarks>
    [HttpPatch("me")]
    [Authorize(Policy = "UserOnly")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), 200)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserProfileRequest request)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(subClaim, out var userId))
        {
            throw new UnauthorizedException();
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        // 更新允許修改的欄位
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.Email != null)
        {
            user.Email = request.Email;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // 重新載入使用者資訊
        var updatedUser = await _dbContext.Users
            .Include(u => u.GroupMemberships)
                .ThenInclude(gm => gm.Group)
            .Include(u => u.Subscriptions)
                .ThenInclude(s => s.MessageType)
            .FirstAsync(u => u.Id == userId);

        var response = new CurrentUserResponse
        {
            Id = updatedUser.Id,
            DisplayName = updatedUser.DisplayName,
            AvatarUrl = updatedUser.PictureUrl,
            Email = updatedUser.Email,
            IsAdmin = updatedUser.IsAdmin,
            Groups = updatedUser.GroupMemberships
                .Where(gm => gm.Group.IsActive)
                .Select(gm => new SimpleGroupResponse
                {
                    Id = gm.Group.Id,
                    Code = gm.Group.Code,
                    Name = gm.Group.Name
                }).ToList(),
            SubscribedMessageTypes = updatedUser.Subscriptions
                .Where(s => s.IsActive)
                .Select(s => s.MessageType)
                .Distinct()
                .Select(mt => new SimpleMessageTypeResponse
                {
                    Code = mt.Code,
                    Name = mt.Name
                }).ToList(),
            CreatedAt = updatedUser.CreatedAt,
            LastLoginAt = updatedUser.LastLoginAt
        };

        return Ok(ApiResponse<CurrentUserResponse>.Ok(response));
    }

    #region Admin Authentication Endpoints

    /// <summary>
    /// 管理員登入
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/admin/login
    /// </remarks>
    [HttpPost("admin/login")]
    [ProducesResponseType(typeof(ApiResponse<AdminLoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var admin = await _adminService.ValidateLoginAsync(request.Username, request.Password);

        if (admin == null)
        {
            // 記錄失敗的登入嘗試
            await _loginLogService.LogAdminLoginAsync(null, false, ipAddress, userAgent, "帳號或密碼錯誤");
            throw new UnauthorizedException("帳號或密碼錯誤");
        }

        // 記錄成功的登入
        await _loginLogService.LogAdminLoginAsync(admin.Id, true, ipAddress, userAgent);

        // 更新最後登入時間
        admin.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // 產生 JWT Token
        var token = _jwtService.GenerateAdminToken(admin.Id, admin.Username, admin.IsSuperAdmin);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var response = new AdminLoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtService.GetExpiresInSeconds(),
            Admin = new AdminResponse
            {
                Id = admin.Id,
                Username = admin.Username,
                DisplayName = admin.DisplayName,
                IsSuperAdmin = admin.IsSuperAdmin,
                MustChangePassword = admin.MustChangePassword,
                LinkedUser = admin.LinkedUser != null ? new SimpleUserResponse
                {
                    Id = admin.LinkedUser.Id,
                    DisplayName = admin.LinkedUser.DisplayName
                } : null,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt
            }
        };

        return Ok(ApiResponse<AdminLoginResponse>.Ok(response));
    }

    /// <summary>
    /// 管理員修改密碼
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/admin/change-password
    /// </remarks>
    [HttpPost("admin/change-password")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> ChangeAdminPassword([FromBody] ChangePasswordRequest request)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(subClaim, out var adminId))
        {
            throw new UnauthorizedException();
        }

        var success = await _adminService.ChangePasswordAsync(adminId, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            throw new BadRequestException(ErrorCodes.INVALID_CREDENTIALS, "目前密碼不正確");
        }

        return Ok(ApiResponse.Ok("密碼已成功更新"));
    }

    #endregion

    #region Private Methods

    private UserResponse MapToUserResponse(Models.User user, bool isNewUser)
    {
        return new UserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.PictureUrl,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            IsNewUser = isNewUser,
            Groups = user.GroupMemberships
                .Where(gm => gm.Group.IsActive)
                .Select(gm => new SimpleGroupResponse
                {
                    Id = gm.Group.Id,
                    Code = gm.Group.Code,
                    Name = gm.Group.Name
                }).ToList(),
            SubscribedMessageTypes = user.Subscriptions
                .Where(s => s.IsActive)
                .Select(s => s.MessageType)
                .Distinct()
                .Select(mt => new SimpleMessageTypeResponse
                {
                    Code = mt.Code,
                    Name = mt.Name
                }).ToList(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private string? GetClientIpAddress()
    {
        // 優先使用 X-Forwarded-For（代理伺服器）
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 其次使用 X-Real-IP
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        // 最後使用連線 IP
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
