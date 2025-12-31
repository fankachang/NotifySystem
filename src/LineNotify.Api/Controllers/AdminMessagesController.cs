using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Controllers;

/// <summary>
/// Admin 訊息管理控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/messages")]
[Authorize(Policy = "AdminOnly")]
public class AdminMessagesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminMessagesController> _logger;

    public AdminMessagesController(AppDbContext dbContext, ILogger<AdminMessagesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 取得訊息列表（管理員用）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AdminMessageListResponse>), 200)]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? messageTypeId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _dbContext.Messages
            .Include(m => m.MessageType)
            .AsQueryable();

        // 篩選條件
        if (!string.IsNullOrEmpty(status))
        {
            // 使用 ProcessedAt 來判斷狀態
            if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.ProcessedAt == null);
            }
            else if (status.Equals("Sent", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.ProcessedAt != null);
            }
        }

        if (messageTypeId.HasValue)
        {
            query = query.Where(m => m.MessageTypeId == messageTypeId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= endDate.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new AdminMessageItem
            {
                Id = m.Id,
                Subject = m.Title,
                Content = m.Content,
                MessageTypeId = m.MessageTypeId,
                MessageTypeName = m.MessageType.Name,
                MessageTypeColor = m.MessageType.Color,
                GroupName = m.TargetGroups,
                Status = m.ProcessedAt != null ? "Sent" : "Pending",
                CreatedAt = m.CreatedAt,
                SentAt = m.ProcessedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<AdminMessageListResponse>.Ok(new AdminMessageListResponse
        {
            Items = messages,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        }));
    }

    /// <summary>
    /// 取得訊息詳情（管理員用）
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AdminMessageDetail>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetMessage(int id)
    {
        var message = await _dbContext.Messages
            .Include(m => m.MessageType)
            .Include(m => m.Deliveries)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null)
        {
            throw new NotFoundException("Message", id);
        }

        return Ok(ApiResponse<AdminMessageDetail>.Ok(new AdminMessageDetail
        {
            Id = message.Id,
            Subject = message.Title,
            Content = message.Content,
            MessageTypeId = message.MessageTypeId,
            MessageTypeName = message.MessageType.Name,
            MessageTypeColor = message.MessageType.Color,
            GroupName = message.TargetGroups,
            Status = message.ProcessedAt != null ? "Sent" : "Pending",
            SourceHost = message.SourceHost,
            SourceService = message.SourceService,
            CreatedAt = message.CreatedAt,
            SentAt = message.ProcessedAt,
            Deliveries = message.Deliveries.Select(d => new DeliveryItem
            {
                UserId = d.UserId,
                UserName = d.User?.DisplayName,
                Status = d.Status.ToString(),
                DeliveredAt = d.SentAt,
                ErrorMessage = d.ErrorMessage,
                RetryCount = d.RetryCount
            }).ToList()
        }));
    }

    /// <summary>
    /// 重試發送訊息
    /// </summary>
    [HttpPost("{id:int}/retry")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RetryMessage(int id)
    {
        var message = await _dbContext.Messages
            .Include(m => m.Deliveries)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null)
        {
            throw new NotFoundException("Message", id);
        }

        // 重設失敗的發送為待發送狀態
        var failedDeliveries = message.Deliveries.Where(d => d.Status == DeliveryStatus.Failed);
        foreach (var delivery in failedDeliveries)
        {
            delivery.Status = DeliveryStatus.Pending;
            delivery.RetryCount = 0;
            delivery.ErrorMessage = null;
            delivery.UpdatedAt = DateTime.UtcNow;
        }

        // 重設訊息的處理時間
        message.ProcessedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("管理員重試發送訊息: MessageId={MessageId}", id);

        return Ok(ApiResponse.Ok("訊息已重新排入發送佇列"));
    }
}

/// <summary>
/// Admin 訊息列表回應
/// </summary>
public class AdminMessageListResponse
{
    /// <summary>訊息列表</summary>
    public List<AdminMessageItem> Items { get; set; } = new();

    /// <summary>當前頁碼</summary>
    public int Page { get; set; }

    /// <summary>每頁數量</summary>
    public int PageSize { get; set; }

    /// <summary>總數量</summary>
    public int TotalCount { get; set; }

    /// <summary>總頁數</summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Admin 訊息項目
/// </summary>
public class AdminMessageItem
{
    /// <summary>訊息 ID</summary>
    public int Id { get; set; }

    /// <summary>主旨</summary>
    public string? Subject { get; set; }

    /// <summary>內容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>訊息類型 ID</summary>
    public int MessageTypeId { get; set; }

    /// <summary>訊息類型名稱</summary>
    public string MessageTypeName { get; set; } = string.Empty;

    /// <summary>訊息類型顏色</summary>
    public string? MessageTypeColor { get; set; }

    /// <summary>群組名稱</summary>
    public string? GroupName { get; set; }

    /// <summary>狀態</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>發送時間</summary>
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Admin 訊息詳情
/// </summary>
public class AdminMessageDetail : AdminMessageItem
{
    /// <summary>來源主機</summary>
    public string? SourceHost { get; set; }

    /// <summary>來源服務</summary>
    public string? SourceService { get; set; }

    /// <summary>派送記錄</summary>
    public List<DeliveryItem> Deliveries { get; set; } = new();
}

/// <summary>
/// 派送記錄項目
/// </summary>
public class DeliveryItem
{
    /// <summary>使用者 ID</summary>
    public int UserId { get; set; }

    /// <summary>使用者名稱</summary>
    public string? UserName { get; set; }

    /// <summary>狀態</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>送達時間</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>錯誤訊息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>重試次數</summary>
    public int RetryCount { get; set; }
}
