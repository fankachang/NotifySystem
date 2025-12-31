using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 登入記錄服務介面
/// </summary>
public interface ILoginLogService
{
    /// <summary>
    /// 記錄使用者登入
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="ipAddress">IP 位址</param>
    /// <param name="userAgent">User Agent</param>
    /// <param name="failureReason">失敗原因（可選）</param>
    Task LogUserLoginAsync(int? userId, bool isSuccess, string? ipAddress, string? userAgent, string? failureReason = null);

    /// <summary>
    /// 記錄管理員登入
    /// </summary>
    /// <param name="adminId">管理員 ID</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="ipAddress">IP 位址</param>
    /// <param name="userAgent">User Agent</param>
    /// <param name="failureReason">失敗原因（可選）</param>
    Task LogAdminLoginAsync(int? adminId, bool isSuccess, string? ipAddress, string? userAgent, string? failureReason = null);

    /// <summary>
    /// 取得使用者的登入記錄
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="count">取得筆數</param>
    /// <returns>登入記錄列表</returns>
    Task<List<LoginLog>> GetUserLoginLogsAsync(int userId, int count = 10);

    /// <summary>
    /// 取得管理員的登入記錄
    /// </summary>
    /// <param name="adminId">管理員 ID</param>
    /// <param name="count">取得筆數</param>
    /// <returns>登入記錄列表</returns>
    Task<List<LoginLog>> GetAdminLoginLogsAsync(int adminId, int count = 10);

    /// <summary>
    /// 檢查登入嘗試次數（用於防止暴力破解）
    /// </summary>
    /// <param name="ipAddress">IP 位址</param>
    /// <param name="withinMinutes">時間範圍（分鐘）</param>
    /// <param name="maxAttempts">最大嘗試次數</param>
    /// <returns>是否允許嘗試</returns>
    Task<bool> CheckLoginAttemptsAsync(string ipAddress, int withinMinutes = 15, int maxAttempts = 5);
}
