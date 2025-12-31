namespace LineNotify.Api.Configuration;

/// <summary>
/// Line API 相關設定
/// </summary>
public class LineSettings
{
    /// <summary>設定區段名稱</summary>
    public const string SectionName = "Line";

    /// <summary>Line Login Channel ID</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>Line Login Channel Secret</summary>
    public string ChannelSecret { get; set; } = string.Empty;

    /// <summary>Line Login 回調 URL</summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>Messaging API Channel Access Token</summary>
    public string MessagingChannelAccessToken { get; set; } = string.Empty;

    /// <summary>Line Login 授權端點</summary>
    public string AuthorizationEndpoint { get; set; } = "https://access.line.me/oauth2/v2.1/authorize";

    /// <summary>Line Token 端點</summary>
    public string TokenEndpoint { get; set; } = "https://api.line.me/oauth2/v2.1/token";

    /// <summary>Line Profile 端點</summary>
    public string ProfileEndpoint { get; set; } = "https://api.line.me/v2/profile";

    /// <summary>Line Messaging API 推送端點</summary>
    public string MessagingPushEndpoint { get; set; } = "https://api.line.me/v2/bot/message/push";

    /// <summary>Line Messaging API 多播端點</summary>
    public string MessagingMulticastEndpoint { get; set; } = "https://api.line.me/v2/bot/message/multicast";
}

/// <summary>
/// JWT 相關設定
/// </summary>
public class JwtSettings
{
    /// <summary>設定區段名稱</summary>
    public const string SectionName = "Jwt";

    /// <summary>JWT 密鑰</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>發行者</summary>
    public string Issuer { get; set; } = "LineNotify";

    /// <summary>受眾</summary>
    public string Audience { get; set; } = "LineNotifyUsers";

    /// <summary>Token 有效期（秒），預設 7 天</summary>
    public int ExpiresInSeconds { get; set; } = 604800;

    /// <summary>Refresh Token 有效期（秒），預設 30 天</summary>
    public int RefreshTokenExpiresInSeconds { get; set; } = 2592000;
}

/// <summary>
/// 應用程式設定
/// </summary>
public class AppSettings
{
    /// <summary>設定區段名稱</summary>
    public const string SectionName = "App";

    /// <summary>每日訊息發送上限</summary>
    public int DailyMessageLimit { get; set; } = 10000;

    /// <summary>API 請求限流（每分鐘）</summary>
    public int RateLimitPerMinute { get; set; } = 100;

    /// <summary>訊息重試次數上限</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>重試間隔（秒）</summary>
    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>資料保留天數</summary>
    public int DataRetentionDays { get; set; } = 90;

    /// <summary>審計日誌保留天數</summary>
    public int AuditLogRetentionDays { get; set; } = 365;
}
