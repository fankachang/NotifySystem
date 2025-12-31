using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 統一 API 回應格式
/// </summary>
/// <typeparam name="T">資料類型</typeparam>
public class ApiResponse<T>
{
    /// <summary>操作是否成功</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>回應訊息</summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>回應資料</summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>錯誤資訊</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorInfo? Error { get; set; }

    /// <summary>警告訊息</summary>
    [JsonPropertyName("warning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Warning { get; set; }

    /// <summary>
    /// 建立成功回應
    /// </summary>
    public static ApiResponse<T> Ok(T? data = default, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// 建立成功回應（帶警告）
    /// </summary>
    public static ApiResponse<T> OkWithWarning(T? data, string warning, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Warning = warning
        };
    }

    /// <summary>
    /// 建立失敗回應
    /// </summary>
    public static ApiResponse<T> Fail(string errorCode, string errorMessage)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ErrorInfo
            {
                Code = errorCode,
                Message = errorMessage
            }
        };
    }

    /// <summary>
    /// 建立失敗回應（帶詳細資訊）
    /// </summary>
    public static ApiResponse<T> Fail(string errorCode, string errorMessage, Dictionary<string, string[]>? details)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ErrorInfo
            {
                Code = errorCode,
                Message = errorMessage,
                Details = details
            }
        };
    }
}

/// <summary>
/// 非泛型 API 回應（用於不需要回傳資料的情況）
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 建立成功回應（無資料）
    /// </summary>
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// 建立失敗回應
    /// </summary>
    public new static ApiResponse Fail(string errorCode, string errorMessage)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ErrorInfo
            {
                Code = errorCode,
                Message = errorMessage
            }
        };
    }
}

/// <summary>
/// 錯誤資訊
/// </summary>
public class ErrorInfo
{
    /// <summary>錯誤代碼</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>錯誤訊息</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>詳細錯誤資訊（欄位驗證錯誤等）</summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Details { get; set; }
}

/// <summary>
/// 分頁資訊
/// </summary>
public class PaginationInfo
{
    /// <summary>當前頁碼</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>每頁筆數</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>總筆數</summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>總頁數</summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    /// <summary>是否有上一頁</summary>
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => Page > 1;

    /// <summary>是否有下一頁</summary>
    [JsonPropertyName("hasNext")]
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// 分頁回應格式
/// </summary>
/// <typeparam name="T">項目類型</typeparam>
public class PagedResponse<T>
{
    /// <summary>項目列表</summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}
