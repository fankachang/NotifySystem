using System.Security.Claims;
using System.Text.Json;
using LineNotify.Api.Data;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Middleware;

/// <summary>
/// 審計日誌中介軟體 - 記錄所有需要審計的 API 操作
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    // 需要審計的 HTTP 方法
    private static readonly HashSet<string> AuditableMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    // 不需要審計的路徑前綴
    private static readonly string[] ExcludedPaths =
    {
        "/health",
        "/api/info",
        "/openapi",
        "/api/v1/auth/line/login",  // 取得登入 URL 不需審計
        "/api/v1/auth/refresh",     // Token 刷新不需審計
        "/api/v1/messages/me"       // 查詢自己的訊息不需審計
    };

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // 檢查是否需要審計
        if (!ShouldAudit(context))
        {
            await _next(context);
            return;
        }

        // 讀取請求內容（用於審計）
        string? requestBody = null;
        if (context.Request.ContentLength > 0 && context.Request.ContentLength < 10240) // 限制 10KB
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // 執行請求
        await _next(context);

        // 只有成功的變更操作才記錄審計日誌
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            await CreateAuditLogAsync(context, dbContext, requestBody);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        // 只審計可修改的 HTTP 方法
        if (!AuditableMethods.Contains(context.Request.Method))
            return false;

        // 排除特定路徑
        var path = context.Request.Path.Value ?? string.Empty;
        foreach (var excludedPath in ExcludedPaths)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // 只審計 API 路徑
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CreateAuditLogAsync(HttpContext context, AppDbContext dbContext, string? requestBody)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = DetermineAction(context),
                EntityType = DetermineEntityType(context),
                EntityId = ExtractEntityId(context),
                NewValues = SanitizeRequestBody(requestBody),
                IpAddress = GetClientIpAddress(context),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // 取得操作者資訊
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userType = user.FindFirst("user_type")?.Value;
                var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

                if (int.TryParse(subClaim, out var id))
                {
                    if (userType == "admin")
                        auditLog.AdminId = id;
                    else
                        auditLog.UserId = id;
                }
            }

            dbContext.AuditLogs.Add(auditLog);
            await dbContext.SaveChangesAsync();

            _logger.LogDebug(
                "審計日誌已記錄: {Action} {EntityType} {EntityId}",
                auditLog.Action,
                auditLog.EntityType,
                auditLog.EntityId);
        }
        catch (Exception ex)
        {
            // 審計日誌失敗不應影響正常請求
            _logger.LogError(ex, "記錄審計日誌失敗");
        }
    }

    private static string DetermineAction(HttpContext context)
    {
        return context.Request.Method.ToUpper() switch
        {
            "POST" => "CREATE",
            "PUT" => "UPDATE",
            "PATCH" => "PARTIAL_UPDATE",
            "DELETE" => "DELETE",
            _ => context.Request.Method.ToUpper()
        };
    }

    private static string DetermineEntityType(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // 解析 API 路徑以確定實體類型
        // 例如: /api/v1/admin/groups -> Group
        //       /api/v1/admin/users -> User
        //       /api/v1/messages/send -> Message

        foreach (var segment in segments.Reverse())
        {
            // 跳過版本號、數字 ID、動作
            if (segment.StartsWith("v") && segment.Length <= 3)
                continue;
            if (int.TryParse(segment, out _))
                continue;
            if (segment is "api" or "admin" or "send" or "members" or "callback")
                continue;

            return segment switch
            {
                "groups" => "Group",
                "users" => "User",
                "admins" => "Admin",
                "api-keys" => "ApiKey",
                "message-types" => "MessageType",
                "messages" => "Message",
                "auth" => "Auth",
                _ => segment
            };
        }

        return "Unknown";
    }

    private static int? ExtractEntityId(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // 尋找路徑中的數字 ID
        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var id))
                return id;
        }

        return null;
    }

    private static string? SanitizeRequestBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            // 嘗試解析並移除敏感欄位
            var json = JsonDocument.Parse(body);
            var sensitiveFields = new[] { "password", "currentPassword", "newPassword", "confirmPassword", "secret", "key", "token" };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteSanitizedJson(writer, json.RootElement, sensitiveFields);
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // 無法解析則回傳原始內容（截斷）
            return body.Length > 500 ? body[..500] + "..." : body;
        }
    }

    private static void WriteSanitizedJson(Utf8JsonWriter writer, JsonElement element, string[] sensitiveFields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (sensitiveFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        writer.WriteString(property.Name, "[REDACTED]");
                    }
                    else
                    {
                        writer.WritePropertyName(property.Name);
                        WriteSanitizedJson(writer, property.Value, sensitiveFields);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSanitizedJson(writer, item, sensitiveFields);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // 優先使用 X-Forwarded-For（代理伺服器）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 其次使用 X-Real-IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        // 最後使用連線 IP
        return context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// 審計日誌中介軟體擴充方法
/// </summary>
public static class AuditLogMiddlewareExtensions
{
    /// <summary>
    /// 使用審計日誌中介軟體
    /// </summary>
    public static IApplicationBuilder UseAuditLog(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLogMiddleware>();
    }
}
