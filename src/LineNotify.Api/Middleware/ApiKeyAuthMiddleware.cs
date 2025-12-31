using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LineNotify.Api.Middleware;

/// <summary>
/// API Key 認證中介軟體
/// 支援 Bearer Token 和 X-API-Key 標頭兩種方式
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    // 需要 API Key 認證的路徑模式
    private static readonly string[] ApiKeyRequiredPaths = new[]
    {
        "/api/v1/messages/send"
    };

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 檢查是否為需要 API Key 認證的路徑
        if (!RequiresApiKeyAuth(path))
        {
            await _next(context);
            return;
        }

        // 嘗試從 Authorization 標頭取得 API Key
        var apiKey = ExtractApiKey(context);

        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteUnauthorizedResponse(context, "缺少 API Key");
            return;
        }

        // 驗證 API Key
        var validatedApiKey = await ValidateApiKeyAsync(dbContext, apiKey);

        if (validatedApiKey == null)
        {
            _logger.LogWarning("無效的 API Key 嘗試存取: {Path}", path);
            await WriteUnauthorizedResponse(context, "API Key 無效或已停用");
            return;
        }

        // 將 API Key 資訊存入 HttpContext
        context.Items["ApiKeyId"] = validatedApiKey.Id;
        context.Items["ApiKeyName"] = validatedApiKey.Name;
        context.Items["ApiKeyCreatedBy"] = validatedApiKey.CreatedByAdminId;

        // 更新最後使用時間
        validatedApiKey.LastUsedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        _logger.LogDebug("API Key '{Name}' 通過認證存取: {Path}", validatedApiKey.Name, path);

        await _next(context);
    }

    /// <summary>
    /// 檢查路徑是否需要 API Key 認證
    /// </summary>
    private static bool RequiresApiKeyAuth(string path)
    {
        return ApiKeyRequiredPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 從請求中提取 API Key
    /// </summary>
    private static string? ExtractApiKey(HttpContext context)
    {
        // 優先從 Authorization: Bearer {key} 標頭取得
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7).Trim();
        }

        // 其次從 X-API-Key 標頭取得
        var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKeyHeader))
        {
            return apiKeyHeader.Trim();
        }

        return null;
    }

    /// <summary>
    /// 驗證 API Key
    /// </summary>
    private async Task<Models.ApiKey?> ValidateApiKeyAsync(AppDbContext dbContext, string providedKey)
    {
        // API Key 格式: {prefix}_{randomPart}
        // 資料庫儲存的是雜湊值，需要比對

        // 計算提供的 Key 的雜湊值
        var hashedKey = ComputeApiKeyHash(providedKey);

        // 查詢資料庫
        var apiKey = await dbContext.ApiKeys
            .Include(k => k.CreatedByAdmin)
            .FirstOrDefaultAsync(k => 
                k.KeyHash == hashedKey && 
                k.IsActive &&
                (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow));

        return apiKey;
    }

    /// <summary>
    /// 計算 API Key 雜湊值（使用十六進位格式，與 ApiKeyService 一致）
    /// </summary>
    public static string ComputeApiKeyHash(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 產生新的 API Key
    /// </summary>
    public static (string Key, string Hash) GenerateApiKey(string prefix = "lnk")
    {
        // 產生 32 位元組的隨機值
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 32);

        var key = $"{prefix}_{randomPart}";
        var hash = ComputeApiKeyHash(key);

        return (key, hash);
    }

    /// <summary>
    /// 寫入未授權回應
    /// </summary>
    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail("INVALID_API_KEY", message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// API Key 認證中介軟體擴充方法
/// </summary>
public static class ApiKeyAuthMiddlewareExtensions
{
    /// <summary>
    /// 使用 API Key 認證中介軟體
    /// </summary>
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
