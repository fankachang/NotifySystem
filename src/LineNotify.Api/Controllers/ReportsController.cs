using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 報表控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// 取得報表摘要
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/summary?startDate=2025-12-01&amp;endDate=2025-12-31
    /// </remarks>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<ReportSummaryResponse>), 200)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _reportService.GetSummaryAsync(startDate, endDate);

        return Ok(ApiResponse<ReportSummaryResponse>.Ok(result));
    }

    /// <summary>
    /// 取得發送統計
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/delivery-stats?days=7
    /// </remarks>
    [HttpGet("delivery-stats")]
    [ProducesResponseType(typeof(ApiResponse<SimpleDeliveryStats>), 200)]
    public async Task<IActionResult> GetDeliveryStats([FromQuery] int days = 7)
    {
        var result = await _reportService.GetSimpleDeliveryStatsAsync(days);

        return Ok(ApiResponse<SimpleDeliveryStats>.Ok(result));
    }

    /// <summary>
    /// 取得系統概覽
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/overview
    /// </remarks>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<SystemOverview>), 200)]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _reportService.GetSystemOverviewAsync();

        return Ok(ApiResponse<SystemOverview>.Ok(result));
    }

    /// <summary>
    /// 取得每日訊息趨勢
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/daily-trend?days=7
    /// </remarks>
    [HttpGet("daily-trend")]
    [ProducesResponseType(typeof(ApiResponse<List<DailyTrendItem>>), 200)]
    public async Task<IActionResult> GetDailyTrend([FromQuery] int days = 7)
    {
        var result = await _reportService.GetDailyTrendAsync(days);

        return Ok(ApiResponse<List<DailyTrendItem>>.Ok(result));
    }

    /// <summary>
    /// 取得訊息類型分布
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/type-distribution?days=7
    /// </remarks>
    [HttpGet("type-distribution")]
    [ProducesResponseType(typeof(ApiResponse<List<TypeDistributionItem>>), 200)]
    public async Task<IActionResult> GetTypeDistribution([FromQuery] int days = 7)
    {
        var result = await _reportService.GetTypeDistributionAsync(days);

        return Ok(ApiResponse<List<TypeDistributionItem>>.Ok(result));
    }

    /// <summary>
    /// 取得熱門群組
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/reports/top-groups?days=7&amp;limit=5
    /// </remarks>
    [HttpGet("top-groups")]
    [ProducesResponseType(typeof(ApiResponse<List<TopGroupItem>>), 200)]
    public async Task<IActionResult> GetTopGroups([FromQuery] int days = 7, [FromQuery] int limit = 5)
    {
        var result = await _reportService.GetTopGroupsAsync(days, limit);

        return Ok(ApiResponse<List<TopGroupItem>>.Ok(result));
    }
}
