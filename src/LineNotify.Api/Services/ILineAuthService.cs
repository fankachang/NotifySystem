using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// Line 認證服務介面
/// </summary>
public interface ILineAuthService
{
    /// <summary>
    /// 取得 Line Login 授權 URL
    /// </summary>
    /// <param name="redirectUri">回調 URI（可選，預設使用設定值）</param>
    /// <returns>授權 URL 和狀態碼</returns>
    Task<(string AuthUrl, string State)> GetAuthorizationUrlAsync(string? redirectUri = null);

    /// <summary>
    /// 處理 Line Login 回調
    /// </summary>
    /// <param name="code">授權碼</param>
    /// <param name="state">狀態碼</param>
    /// <param name="expectedState">預期的狀態碼</param>
    /// <returns>使用者資訊和是否為新使用者</returns>
    Task<(User User, bool IsNewUser)> HandleCallbackAsync(string code, string state, string? expectedState = null);

    /// <summary>
    /// 驗證狀態碼
    /// </summary>
    /// <param name="state">狀態碼</param>
    /// <param name="expectedState">預期的狀態碼</param>
    /// <returns>是否有效</returns>
    bool ValidateState(string state, string expectedState);

    /// <summary>
    /// 刷新使用者的 Line 存取權杖
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RefreshLineAccessTokenAsync(int userId);

    /// <summary>
    /// 驗證使用者的 Line 綁定是否有效
    /// </summary>
    /// <param name="lineUserId">Line 使用者 ID</param>
    /// <returns>是否有效</returns>
    Task<bool> ValidateLineBindingAsync(string lineUserId);
}

/// <summary>
/// Line 使用者資訊（從 Line API 取得）
/// </summary>
public class LineUserProfile
{
    /// <summary>Line User ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>顯示名稱</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>頭像 URL</summary>
    public string? PictureUrl { get; set; }

    /// <summary>狀態訊息</summary>
    public string? StatusMessage { get; set; }
}

/// <summary>
/// Line OAuth Token 回應
/// </summary>
public class LineTokenResponse
{
    /// <summary>存取權杖</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>權杖類型</summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>刷新權杖</summary>
    public string? RefreshToken { get; set; }

    /// <summary>有效期（秒）</summary>
    public int ExpiresIn { get; set; }

    /// <summary>ID Token</summary>
    public string? IdToken { get; set; }

    /// <summary>授權範圍</summary>
    public string? Scope { get; set; }
}
