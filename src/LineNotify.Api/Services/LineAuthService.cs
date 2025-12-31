using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using LineNotify.Api.Configuration;
using LineNotify.Api.Data;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LineNotify.Api.Services;

/// <summary>
/// Line 認證服務實作
/// </summary>
public class LineAuthService : ILineAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LineSettings _lineSettings;
    private readonly ILogger<LineAuthService> _logger;

    public LineAuthService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<LineSettings> lineSettings,
        ILogger<LineAuthService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _lineSettings = lineSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<(string AuthUrl, string State)> GetAuthorizationUrlAsync(string? redirectUri = null)
    {
        // 產生隨機狀態碼
        var state = GenerateState();

        // 建構授權 URL
        var callbackUrl = redirectUri ?? _lineSettings.CallbackUrl;
        var scope = "profile openid";

        var authUrl = $"{_lineSettings.AuthorizationEndpoint}" +
            $"?response_type=code" +
            $"&client_id={_lineSettings.ChannelId}" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
            $"&state={state}" +
            $"&scope={Uri.EscapeDataString(scope)}";

        return Task.FromResult((authUrl, state));
    }

    /// <inheritdoc/>
    public async Task<(User User, bool IsNewUser)> HandleCallbackAsync(string code, string state, string? expectedState = null)
    {
        // 驗證狀態碼（如果提供）
        if (!string.IsNullOrEmpty(expectedState) && !ValidateState(state, expectedState))
        {
            throw new BadRequestException(ErrorCodes.INVALID_STATE, "狀態碼不匹配");
        }

        // 使用授權碼換取存取權杖
        var tokenResponse = await ExchangeCodeForTokenAsync(code);

        // 使用存取權杖取得使用者資訊
        var lineProfile = await GetUserProfileAsync(tokenResponse.AccessToken);

        // 查詢或建立使用者
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.LineUserId == lineProfile.UserId);

        bool isNewUser = existingUser == null;

        if (isNewUser)
        {
            // 建立新使用者
            var newUser = new User
            {
                LineUserId = lineProfile.UserId,
                DisplayName = lineProfile.DisplayName,
                PictureUrl = lineProfile.PictureUrl,
                LineAccessToken = tokenResponse.AccessToken,
                LineAccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("新使用者註冊: {LineUserId} - {DisplayName}", lineProfile.UserId, lineProfile.DisplayName);

            return (newUser, true);
        }
        else
        {
            // 更新現有使用者
            existingUser!.DisplayName = lineProfile.DisplayName;
            existingUser.PictureUrl = lineProfile.PictureUrl;
            existingUser.LineAccessToken = tokenResponse.AccessToken;
            existingUser.LineAccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            existingUser.UpdatedAt = DateTime.UtcNow;
            existingUser.LastLoginAt = DateTime.UtcNow;

            // 檢查帳號狀態
            if (!existingUser.IsActive)
            {
                throw new ForbiddenException(ErrorCodes.USER_DISABLED, "使用者帳號已停用");
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("使用者登入: {LineUserId} - {DisplayName}", lineProfile.UserId, lineProfile.DisplayName);

            return (existingUser, false);
        }
    }

    /// <inheritdoc/>
    public bool ValidateState(string state, string expectedState)
    {
        return !string.IsNullOrEmpty(state) &&
               !string.IsNullOrEmpty(expectedState) &&
               state == expectedState;
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshLineAccessTokenAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return false;

        // Line Login 使用的是短期 Token，無法刷新
        // 需要使用者重新登入
        // 這裡只是標記 Token 已過期
        if (user.LineAccessTokenExpiresAt < DateTime.UtcNow)
        {
            user.LineAccessToken = null;
            user.LineAccessTokenExpiresAt = null;
            await _dbContext.SaveChangesAsync();
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateLineBindingAsync(string lineUserId)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.LineUserId == lineUserId);

            if (user == null || string.IsNullOrEmpty(user.LineAccessToken))
                return false;

            // 嘗試呼叫 Line API 驗證 Token
            var profile = await GetUserProfileAsync(user.LineAccessToken);
            return profile.UserId == lineUserId;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private static string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private async Task<LineTokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        var client = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _lineSettings.CallbackUrl,
            ["client_id"] = _lineSettings.ChannelId,
            ["client_secret"] = _lineSettings.ChannelSecret
        });

        var response = await client.PostAsync(_lineSettings.TokenEndpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Line Token 交換失敗: {StatusCode} - {Response}",
                response.StatusCode, responseContent);
            throw new BadRequestException(ErrorCodes.INVALID_CODE, "授權碼無效或已過期");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var tokenResponse = JsonSerializer.Deserialize<LineTokenResponse>(responseContent, options);
        if (tokenResponse == null)
        {
            throw new BadRequestException(ErrorCodes.INVALID_CODE, "無法解析 Line Token 回應");
        }

        return tokenResponse;
    }

    private async Task<LineUserProfile> GetUserProfileAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(_lineSettings.ProfileEndpoint);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("取得 Line 使用者資訊失敗: {StatusCode} - {Response}",
                response.StatusCode, responseContent);
            throw new ServiceUnavailableException("無法取得 Line 使用者資訊");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var profile = JsonSerializer.Deserialize<LineUserProfile>(responseContent, options);
        if (profile == null)
        {
            throw new ServiceUnavailableException("無法解析 Line 使用者資訊");
        }

        return profile;
    }

    #endregion
}
