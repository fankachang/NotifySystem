using System.Text.Json.Serialization;

namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// 報表摘要回應
/// </summary>
public class ReportSummaryResponse
{
    /// <summary>總訊息數</summary>
    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    /// <summary>總發送數</summary>
    [JsonPropertyName("totalDeliveries")]
    public int TotalDeliveries { get; set; }

    /// <summary>成功率 (%)</summary>
    [JsonPropertyName("successRate")]
    public double SuccessRate { get; set; }

    /// <summary>依訊息類型統計</summary>
    [JsonPropertyName("byMessageType")]
    public List<MessageTypeStatItem> ByMessageType { get; set; } = new();

    /// <summary>依日期統計</summary>
    [JsonPropertyName("byDay")]
    public List<DailyStatItem> ByDay { get; set; } = new();

    /// <summary>尖峰時段 (小時)</summary>
    [JsonPropertyName("peakHours")]
    public List<int> PeakHours { get; set; } = new();
}

/// <summary>
/// 訊息類型統計項目
/// </summary>
public class MessageTypeStatItem
{
    /// <summary>訊息類型代碼</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>訊息類型名稱</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>數量</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>顏色</summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#808080";
}

/// <summary>
/// 每日統計項目
/// </summary>
public class DailyStatItem
{
    /// <summary>日期</summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>數量</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// 發送統計回應
/// </summary>
public class DeliveryStatsResponse
{
    /// <summary>統計期間開始</summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>統計期間結束</summary>
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    /// <summary>總發送數</summary>
    [JsonPropertyName("totalDeliveries")]
    public int TotalDeliveries { get; set; }

    /// <summary>發送成功數</summary>
    [JsonPropertyName("sentCount")]
    public int SentCount { get; set; }

    /// <summary>發送失敗數</summary>
    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    /// <summary>待發送數</summary>
    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }

    /// <summary>跳過數</summary>
    [JsonPropertyName("skippedCount")]
    public int SkippedCount { get; set; }

    /// <summary>平均重試次數</summary>
    [JsonPropertyName("avgRetryCount")]
    public double AvgRetryCount { get; set; }

    /// <summary>依狀態統計</summary>
    [JsonPropertyName("byStatus")]
    public List<StatusStatItem> ByStatus { get; set; } = new();

    /// <summary>依小時統計</summary>
    [JsonPropertyName("byHour")]
    public List<HourlyStatItem> ByHour { get; set; } = new();
}

/// <summary>
/// 狀態統計項目
/// </summary>
public class StatusStatItem
{
    /// <summary>狀態</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>數量</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>百分比</summary>
    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }
}

/// <summary>
/// 每小時統計項目
/// </summary>
public class HourlyStatItem
{
    /// <summary>小時 (0-23)</summary>
    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    /// <summary>數量</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
