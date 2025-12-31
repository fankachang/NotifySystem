using System.Security.Claims;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// API Key 管理控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/api-keys")]
[Authorize(Policy = "AdminOnly")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>取得 API Key 列表</summary>
    /// <param name="page">頁碼（預設 1）</param>
    /// <param name="pageSize">每頁筆數（預設 20）</param>
    /// <param name="search">搜尋關鍵字（名稱、描述、前綴）</param>
    /// <param name="isActive">篩選啟用狀態</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiKeyListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var response = await _apiKeyService.GetAllAsync(page, pageSize, search, isActive);
        return Ok(response);
    }

    /// <summary>取得 API Key 詳細資訊</summary>
    /// <param name="id">API Key ID</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiKeyDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _apiKeyService.GetByIdAsync(id);
        if (response == null)
            return NotFound(new { message = "找不到指定的 API Key" });

        return Ok(response);
    }

    /// <summary>建立新的 API Key</summary>
    /// <param name="request">建立請求</param>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 從 JWT 取得管理員 ID
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (adminIdClaim == null || !int.TryParse(adminIdClaim.Value, out var adminId))
        {
            return Unauthorized(new { message = "無法識別管理員身分" });
        }

        var response = await _apiKeyService.CreateAsync(request, adminId);
        _logger.LogInformation("管理員 {AdminId} 建立了新的 API Key: {KeyName}", adminId, request.Name);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>更新 API Key</summary>
    /// <param name="id">API Key ID</param>
    /// <param name="request">更新請求</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiKeyDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _apiKeyService.UpdateAsync(id, request);
        if (response == null)
            return NotFound(new { message = "找不到指定的 API Key" });

        return Ok(response);
    }

    /// <summary>撤銷 API Key（停用）</summary>
    /// <param name="id">API Key ID</param>
    [HttpPost("{id:int}/revoke")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Revoke(int id)
    {
        var success = await _apiKeyService.RevokeAsync(id);
        if (!success)
            return NotFound(new { message = "找不到指定的 API Key" });

        return Ok(new { message = "API Key 已撤銷" });
    }

    /// <summary>刪除 API Key</summary>
    /// <param name="id">API Key ID</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _apiKeyService.DeleteAsync(id);
        if (!success)
            return NotFound(new { message = "找不到指定的 API Key" });

        return NoContent();
    }

    /// <summary>取得統計資訊</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiKeyStatsResponse), 200)]
    public async Task<IActionResult> GetStats()
    {
        var all = await _apiKeyService.GetAllAsync(1, int.MaxValue);
        var now = DateTime.UtcNow;

        var stats = new ApiKeyStatsResponse
        {
            TotalCount = all.Pagination.TotalItems,
            ActiveCount = all.Items.Count(k => k.IsActive && !k.IsExpired),
            ExpiredCount = all.Items.Count(k => k.IsExpired),
            RevokedCount = all.Items.Count(k => !k.IsActive),
            TotalUsage = all.Items.Sum(k => k.UsageCount),
            RecentlyUsed = all.Items.Count(k => k.LastUsedAt.HasValue && k.LastUsedAt > now.AddDays(-7))
        };

        return Ok(stats);
    }
}

/// <summary>
/// API Key 統計回應
/// </summary>
public class ApiKeyStatsResponse
{
    /// <summary>總數</summary>
    public int TotalCount { get; set; }

    /// <summary>啟用中數量</summary>
    public int ActiveCount { get; set; }

    /// <summary>已過期數量</summary>
    public int ExpiredCount { get; set; }

    /// <summary>已撤銷數量</summary>
    public int RevokedCount { get; set; }

    /// <summary>總使用次數</summary>
    public long TotalUsage { get; set; }

    /// <summary>近 7 天有使用過的數量</summary>
    public int RecentlyUsed { get; set; }
}
