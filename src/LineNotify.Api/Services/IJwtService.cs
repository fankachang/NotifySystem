namespace LineNotify.Api.Services;

/// <summary>
/// JWT Token 服務介面
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 產生使用者 JWT Token
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="displayName">顯示名稱</param>
    /// <param name="isAdmin">是否為管理員</param>
    /// <param name="claims">額外的 Claims</param>
    /// <returns>JWT Token 字串</returns>
    string GenerateUserToken(int userId, string displayName, bool isAdmin = false, Dictionary<string, string>? claims = null);

    /// <summary>
    /// 產生管理員 JWT Token
    /// </summary>
    /// <param name="adminId">管理員 ID</param>
    /// <param name="username">帳號名稱</param>
    /// <param name="isSuperAdmin">是否為超級管理員</param>
    /// <returns>JWT Token 字串</returns>
    string GenerateAdminToken(int adminId, string username, bool isSuperAdmin = false);

    /// <summary>
    /// 產生 Refresh Token
    /// </summary>
    /// <returns>Refresh Token 字串</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// 驗證 JWT Token
    /// </summary>
    /// <param name="token">JWT Token 字串</param>
    /// <returns>Token 是否有效</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// 從 Token 取得使用者 ID
    /// </summary>
    /// <param name="token">JWT Token 字串</param>
    /// <returns>使用者 ID，若無效則回傳 null</returns>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// 從 Token 取得管理員 ID
    /// </summary>
    /// <param name="token">JWT Token 字串</param>
    /// <returns>管理員 ID，若無效則回傳 null</returns>
    int? GetAdminIdFromToken(string token);

    /// <summary>
    /// 取得 Token 過期時間（秒）
    /// </summary>
    /// <returns>過期時間（秒）</returns>
    int GetExpiresInSeconds();
}
