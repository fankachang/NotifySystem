namespace LineNotify.Api.Exceptions;

/// <summary>
/// API 例外基底類別
/// </summary>
public class ApiException : Exception
{
    /// <summary>HTTP 狀態碼</summary>
    public int StatusCode { get; }

    /// <summary>錯誤代碼</summary>
    public string ErrorCode { get; }

    /// <summary>詳細錯誤資訊</summary>
    public Dictionary<string, string[]>? Details { get; }

    public ApiException(int statusCode, string errorCode, string message, Dictionary<string, string[]>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Details = details;
    }
}

/// <summary>
/// 400 Bad Request 例外
/// </summary>
public class BadRequestException : ApiException
{
    public BadRequestException(string errorCode, string message, Dictionary<string, string[]>? details = null)
        : base(400, errorCode, message, details)
    {
    }
}

/// <summary>
/// 401 Unauthorized 例外
/// </summary>
public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "未授權的存取")
        : base(401, "UNAUTHORIZED", message)
    {
    }
}

/// <summary>
/// 403 Forbidden 例外
/// </summary>
public class ForbiddenException : ApiException
{
    public ForbiddenException(string errorCode, string message)
        : base(403, errorCode, message)
    {
    }
}

/// <summary>
/// 404 Not Found 例外
/// </summary>
public class NotFoundException : ApiException
{
    public NotFoundException(string resourceType, object resourceId)
        : base(404, "NOT_FOUND", $"{resourceType} '{resourceId}' 不存在")
    {
    }

    public NotFoundException(string message)
        : base(404, "NOT_FOUND", message)
    {
    }
}

/// <summary>
/// 409 Conflict 例外
/// </summary>
public class ConflictException : ApiException
{
    public ConflictException(string errorCode, string message)
        : base(409, errorCode, message)
    {
    }
}

/// <summary>
/// 422 Unprocessable Entity 例外
/// </summary>
public class ValidationException : ApiException
{
    public ValidationException(string message, Dictionary<string, string[]>? details = null)
        : base(422, "VALIDATION_ERROR", message, details)
    {
    }
}

/// <summary>
/// 429 Too Many Requests 例外
/// </summary>
public class RateLimitExceededException : ApiException
{
    /// <summary>重試前需等待的秒數</summary>
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds = 60)
        : base(429, "RATE_LIMITED", "請求過於頻繁，請稍後再試")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

/// <summary>
/// 503 Service Unavailable 例外
/// </summary>
public class ServiceUnavailableException : ApiException
{
    public ServiceUnavailableException(string message = "服務暫時無法使用")
        : base(503, "SERVICE_UNAVAILABLE", message)
    {
    }
}

/// <summary>
/// 錯誤代碼常數
/// </summary>
public static class ErrorCodes
{
    // 認證相關
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string INVALID_CODE = "INVALID_CODE";
    public const string INVALID_STATE = "INVALID_STATE";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
    public const string USER_DISABLED = "USER_DISABLED";
    public const string ADMIN_DISABLED = "ADMIN_DISABLED";
    public const string TOO_MANY_ATTEMPTS = "TOO_MANY_ATTEMPTS";
    public const string PASSWORD_CHANGE_REQUIRED = "PASSWORD_CHANGE_REQUIRED";

    // 請求相關
    public const string INVALID_REQUEST = "INVALID_REQUEST";
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string MISSING_REQUIRED_FIELD = "MISSING_REQUIRED_FIELD";

    // 資源相關
    public const string NOT_FOUND = "NOT_FOUND";
    public const string ALREADY_EXISTS = "ALREADY_EXISTS";
    public const string DUPLICATE_ENTRY = "DUPLICATE_ENTRY";
    public const string CANNOT_DELETE = "CANNOT_DELETE";

    // 權限相關
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";

    // API Key 相關
    public const string INVALID_API_KEY = "INVALID_API_KEY";
    public const string API_KEY_EXPIRED = "API_KEY_EXPIRED";
    public const string API_KEY_DISABLED = "API_KEY_DISABLED";

    // 訊息相關
    public const string INVALID_MESSAGE_TYPE = "INVALID_MESSAGE_TYPE";
    public const string MESSAGE_TOO_LONG = "MESSAGE_TOO_LONG";
    public const string NO_RECIPIENTS = "NO_RECIPIENTS";

    // 系統相關
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string RATE_LIMITED = "RATE_LIMITED";
    public const string LINE_API_ERROR = "LINE_API_ERROR";
}
