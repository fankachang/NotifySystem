using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using LineNotify.Api.DTOs.Responses;
using Microsoft.Extensions.Options;

namespace LineNotify.Api.Middleware;

/// <summary>
/// Rate Limiting 設定
/// </summary>
public class RateLimitingSettings
{
    /// <summary>是否啟用 Rate Limiting</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>時間窗口（秒）</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>每個時間窗口內允許的最大請求數</summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>API Key 的 Rate Limit 設定（覆蓋預設值）</summary>
    public Dictionary<string, int> ApiKeyLimits { get; set; } = new();

    /// <summary>路徑特定的 Rate Limit 設定</summary>
    public Dictionary<string, PathRateLimit> PathLimits { get; set; } = new();

    /// <summary>白名單 IP（不受 Rate Limit 限制）</summary>
    public List<string> WhitelistedIps { get; set; } = new();

    /// <summary>白名單 API Keys（不受 Rate Limit 限制）</summary>
    public List<string> WhitelistedApiKeys { get; set; } = new();
}

/// <summary>
/// 路徑特定的 Rate Limit 設定
/// </summary>
public class PathRateLimit
{
    /// <summary>時間窗口（秒）</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>每個時間窗口內允許的最大請求數</summary>
    public int MaxRequests { get; set; } = 100;
}

/// <summary>
/// Rate Limiting 中介軟體
/// 使用滑動視窗演算法限制 API 請求頻率
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingSettings _settings;

    // 使用 ConcurrentDictionary 儲存請求計數
    private static readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();

    // 清理計時器
    private static Timer? _cleanupTimer;
    private static readonly object _cleanupLock = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitingSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;

        // 啟動清理計時器（每分鐘清理過期的計數器）
        lock (_cleanupLock)
        {
            _cleanupTimer ??= new Timer(CleanupExpiredCounters, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        // 取得客戶端識別碼（優先使用 API Key，否則使用 IP）
        var clientId = GetClientIdentifier(context);

        // 檢查白名單
        if (IsWhitelisted(context, clientId))
        {
            await _next(context);
            return;
        }

        // 取得適用的 Rate Limit 設定
        var (windowSeconds, maxRequests) = GetRateLimitSettings(context, clientId);

        // 取得或建立計數器
        var counter = _counters.GetOrAdd(clientId, _ => new RateLimitCounter());

        // 檢查是否超過限制
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-windowSeconds);

        bool isRateLimited;
        int retryAfter = windowSeconds;

        lock (counter)
        {
            // 清除過期的請求記錄
            counter.CleanupOldRequests(windowStart);

            // 檢查是否超過限制
            if (counter.RequestCount >= maxRequests)
            {
                isRateLimited = true;
                // 計算重試時間
                var oldestRequest = counter.GetOldestRequestTime();
                retryAfter = oldestRequest.HasValue
                    ? (int)Math.Ceiling((oldestRequest.Value.AddSeconds(windowSeconds) - now).TotalSeconds)
                    : windowSeconds;
            }
            else
            {
                isRateLimited = false;
                // 記錄請求
                counter.AddRequest(now);
            }
        }

        // 處理超過限制的情況（在 lock 外部）
        if (isRateLimited)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}, Requests: {Count}/{Max}",
                clientId, counter.RequestCount, maxRequests);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = (DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds()).ToString();

            var response = ApiResponse<object>.Fail(
                "RATE_LIMIT_EXCEEDED",
                $"請求過於頻繁，請在 {retryAfter} 秒後重試");

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
            return;
        }

        // 添加 Rate Limit 標頭
        context.Response.OnStarting(() =>
        {
            var remaining = Math.Max(0, maxRequests - counter.RequestCount);
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = (DateTimeOffset.UtcNow.AddSeconds(windowSeconds).ToUnixTimeSeconds()).ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// 取得客戶端識別碼
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // 優先使用 API Key
        if (context.Items.TryGetValue("ApiKeyId", out var apiKeyId) && apiKeyId != null)
        {
            return $"apikey:{apiKeyId}";
        }

        // 使用 IP 地址
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 考慮 X-Forwarded-For 標頭（反向代理情況）
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                ip = ips[0].Trim();
            }
        }

        return $"ip:{ip}";
    }

    /// <summary>
    /// 檢查是否在白名單中
    /// </summary>
    private bool IsWhitelisted(HttpContext context, string clientId)
    {
        // 檢查 IP 白名單
        if (clientId.StartsWith("ip:"))
        {
            var ip = clientId.Substring(3);
            if (_settings.WhitelistedIps.Contains(ip))
            {
                return true;
            }
        }

        // 檢查 API Key 白名單
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            if (_settings.WhitelistedApiKeys.Contains(apiKey.ToString()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 取得適用的 Rate Limit 設定
    /// </summary>
    private (int windowSeconds, int maxRequests) GetRateLimitSettings(HttpContext context, string clientId)
    {
        var path = context.Request.Path.Value ?? "";

        // 檢查路徑特定設定
        foreach (var (pattern, limit) in _settings.PathLimits)
        {
            if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return (limit.WindowSeconds, limit.MaxRequests);
            }
        }

        // 檢查 API Key 特定設定
        if (clientId.StartsWith("apikey:"))
        {
            var keyId = clientId.Substring(7);
            if (_settings.ApiKeyLimits.TryGetValue(keyId, out var maxRequests))
            {
                return (_settings.WindowSeconds, maxRequests);
            }
        }

        // 使用預設設定
        return (_settings.WindowSeconds, _settings.MaxRequests);
    }

    /// <summary>
    /// 清理過期的計數器
    /// </summary>
    private static void CleanupExpiredCounters(object? state)
    {
        var expiredTime = DateTime.UtcNow.AddMinutes(-5);

        foreach (var kvp in _counters)
        {
            if (kvp.Value.LastRequestTime < expiredTime)
            {
                _counters.TryRemove(kvp.Key, out _);
            }
        }
    }
}

/// <summary>
/// Rate Limit 計數器
/// </summary>
internal class RateLimitCounter
{
    private readonly List<DateTime> _requestTimes = new();

    public int RequestCount => _requestTimes.Count;
    public DateTime LastRequestTime { get; private set; } = DateTime.MinValue;

    public void AddRequest(DateTime time)
    {
        _requestTimes.Add(time);
        LastRequestTime = time;
    }

    public void CleanupOldRequests(DateTime windowStart)
    {
        _requestTimes.RemoveAll(t => t < windowStart);
    }

    public DateTime? GetOldestRequestTime()
    {
        return _requestTimes.Count > 0 ? _requestTimes.Min() : null;
    }
}

/// <summary>
/// Rate Limiting 擴充方法
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingSettings>(configuration.GetSection("RateLimiting"));
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
