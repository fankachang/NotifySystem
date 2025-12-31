using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 審計日誌控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(AppDbContext dbContext, ILogger<AuditLogsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 取得審計日誌列表
    /// </summary>
    /// <remarks>
    /// GET /api/v1/admin/audit-logs?page=1&amp;pageSize=20&amp;adminId=1&amp;action=CREATE&amp;entityType=Group
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AuditLogListResponse>), 200)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? adminId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        // 限制每頁最大筆數
        pageSize = Math.Min(pageSize, 100);

        var query = _dbContext.AuditLogs
            .Include(a => a.Admin)
            .AsQueryable();

        // 管理員篩選
        if (adminId.HasValue)
        {
            query = query.Where(a => a.AdminId == adminId.Value);
        }

        // 操作類型篩選
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        // 實體類型篩選
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        // 日期範圍篩選
        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value);
        }

        // 計算總數
        var totalItems = await query.CountAsync();

        // 分頁查詢
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 轉換為回應 DTO
        var items = logs.Select(a => new AuditLogItem
        {
            Id = a.Id,
            Admin = a.Admin != null ? new SimpleAdminResponse
            {
                Id = a.Admin.Id,
                Username = a.Admin.Username
            } : null,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValue = a.OldValues != null ? System.Text.Json.JsonSerializer.Deserialize<object>(a.OldValues) : null,
            NewValue = a.NewValues != null ? System.Text.Json.JsonSerializer.Deserialize<object>(a.NewValues) : null,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        }).ToList();

        var response = new AuditLogListResponse
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };

        return Ok(ApiResponse<AuditLogListResponse>.Ok(response));
    }
}
