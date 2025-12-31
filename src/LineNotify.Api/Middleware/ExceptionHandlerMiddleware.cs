using System.Net;
using System.Text.Json;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;

namespace LineNotify.Api.Middleware;

/// <summary>
/// 全域例外處理中介軟體
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse<object> apiResponse;

        switch (exception)
        {
            case ApiException apiException:
                response.StatusCode = apiException.StatusCode;
                apiResponse = ApiResponse<object>.Fail(
                    apiException.ErrorCode,
                    apiException.Message,
                    apiException.Details);

                // 處理 Rate Limit 例外的 Retry-After 標頭
                if (apiException is RateLimitExceededException rateLimitEx)
                {
                    response.Headers.Append("Retry-After", rateLimitEx.RetryAfterSeconds.ToString());
                }

                _logger.LogWarning(
                    "API 例外: {StatusCode} {ErrorCode} - {Message}",
                    apiException.StatusCode,
                    apiException.ErrorCode,
                    apiException.Message);
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse = ApiResponse<object>.Fail(ErrorCodes.UNAUTHORIZED, "未授權的存取");
                _logger.LogWarning("未授權存取: {Path}", context.Request.Path);
                break;

            case OperationCanceledException:
                response.StatusCode = 499; // Client Closed Request
                apiResponse = ApiResponse<object>.Fail("REQUEST_CANCELLED", "請求已取消");
                _logger.LogInformation("請求已取消: {Path}", context.Request.Path);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var message = _env.IsDevelopment()
                    ? exception.Message
                    : "發生內部錯誤，請稍後再試";

                apiResponse = ApiResponse<object>.Fail(ErrorCodes.INTERNAL_ERROR, message);

                _logger.LogError(
                    exception,
                    "未處理的例外: {Message} - Path: {Path}",
                    exception.Message,
                    context.Request.Path);
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        await response.WriteAsJsonAsync(apiResponse, jsonOptions);
    }
}

/// <summary>
/// 中介軟體擴充方法
/// </summary>
public static class ExceptionHandlerMiddlewareExtensions
{
    /// <summary>
    /// 使用全域例外處理中介軟體
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}
