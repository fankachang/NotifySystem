using LineNotify.Api.DTOs.Responses;

namespace LineNotify.Api.Services;

/// <summary>
/// 報表服務介面
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 取得報表摘要
    /// </summary>
    /// <param name="startDate">開始日期</param>
    /// <param name="endDate">結束日期</param>
    /// <returns>報表摘要</returns>
    Task<ReportSummaryResponse> GetSummaryAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// 取得發送統計
    /// </summary>
    /// <param name="startDate">開始日期</param>
    /// <param name="endDate">結束日期</param>
    /// <returns>發送統計</returns>
    Task<DeliveryStatsResponse> GetDeliveryStatsAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// 取得系統概覽
    /// </summary>
    /// <returns>系統概覽數據</returns>
    Task<SystemOverview> GetSystemOverviewAsync();

    /// <summary>
    /// 取得每日訊息趨勢
    /// </summary>
    /// <param name="days">天數</param>
    /// <returns>每日訊息統計</returns>
    Task<List<DailyTrendItem>> GetDailyTrendAsync(int days);

    /// <summary>
    /// 取得訊息類型分布
    /// </summary>
    /// <param name="days">天數</param>
    /// <returns>訊息類型分布統計</returns>
    Task<List<TypeDistributionItem>> GetTypeDistributionAsync(int days);

    /// <summary>
    /// 取得熱門群組
    /// </summary>
    /// <param name="days">天數</param>
    /// <param name="limit">數量限制</param>
    /// <returns>熱門群組列表</returns>
    Task<List<TopGroupItem>> GetTopGroupsAsync(int days, int limit);

    /// <summary>
    /// 取得簡化的發送統計（給 Dashboard 用）
    /// </summary>
    /// <param name="days">天數</param>
    /// <returns>簡化的發送統計</returns>
    Task<SimpleDeliveryStats> GetSimpleDeliveryStatsAsync(int days);
}

/// <summary>
/// 系統概覽
/// </summary>
public class SystemOverview
{
    /// <summary>總使用者數</summary>
    public int TotalUsers { get; set; }

    /// <summary>活躍使用者數</summary>
    public int ActiveUsers { get; set; }

    /// <summary>總群組數</summary>
    public int TotalGroups { get; set; }

    /// <summary>總訊息類型數</summary>
    public int TotalMessageTypes { get; set; }

    /// <summary>今日訊息數</summary>
    public int TodayMessages { get; set; }

    /// <summary>今日發送數</summary>
    public int TodayDeliveries { get; set; }

    /// <summary>待處理發送數</summary>
    public int PendingDeliveries { get; set; }

    /// <summary>今日新用戶數</summary>
    public int NewUsersToday { get; set; }
}

/// <summary>
/// 每日趨勢項目
/// </summary>
public class DailyTrendItem
{
    /// <summary>日期</summary>
    public DateTime Date { get; set; }

    /// <summary>數量</summary>
    public int Count { get; set; }
}

/// <summary>
/// 訊息類型分布項目
/// </summary>
public class TypeDistributionItem
{
    /// <summary>訊息類型 ID</summary>
    public int Id { get; set; }

    /// <summary>代碼</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>顏色</summary>
    public string? Color { get; set; }

    /// <summary>圖示</summary>
    public string? Icon { get; set; }

    /// <summary>數量</summary>
    public int Count { get; set; }
}

/// <summary>
/// 熱門群組項目
/// </summary>
public class TopGroupItem
{
    /// <summary>群組 ID</summary>
    public int GroupId { get; set; }

    /// <summary>群組名稱</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>訊息數量</summary>
    public int MessageCount { get; set; }
}

/// <summary>
/// 簡化的發送統計
/// </summary>
public class SimpleDeliveryStats
{
    /// <summary>總數</summary>
    public int Total { get; set; }

    /// <summary>待發送</summary>
    public int Pending { get; set; }

    /// <summary>已發送</summary>
    public int Sent { get; set; }

    /// <summary>失敗</summary>
    public int Failed { get; set; }
}
