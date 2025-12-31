using System.Security.Claims;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 訊息控制器 - 處理訊息發送和查詢
/// </summary>
[ApiController]
[Route("api/v1/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageDispatchService _messageDispatchService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageDispatchService messageDispatchService,
        ILogger<MessagesController> logger)
    {
        _messageDispatchService = messageDispatchService;
        _logger = logger;
    }

    /// <summary>
    /// 發送訊息
    /// </summary>
    /// <remarks>
    /// POST /api/v1/messages/send
    /// 
    /// 認證方式：API Key (Bearer Token 或 X-API-Key 標頭)
    /// </remarks>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<SendMessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        // 從 HttpContext 取得 API Key 資訊（由 ApiKeyAuthMiddleware 設定）
        var apiKeyId = HttpContext.Items["ApiKeyId"] as int?;

        if (!apiKeyId.HasValue)
        {
            throw new UnauthorizedException("API Key 認證失敗");
        }

        _logger.LogInformation("收到發送訊息請求: Type={MessageType}, Title={Title}, ApiKey={ApiKeyId}",
            request.MessageType, request.Title, apiKeyId.Value);

        var result = await _messageDispatchService.DispatchMessageAsync(request, apiKeyId.Value);

        if (!result.Success)
        {
            throw new BadRequestException(result.ErrorCode ?? "DISPATCH_FAILED", result.ErrorMessage ?? "訊息發送失敗");
        }

        var response = new SendMessageResponse
        {
            MessageId = result.MessageId,
            RecipientCount = result.RecipientCount,
            Status = result.Status
        };

        return Ok(ApiResponse<SendMessageResponse>.Ok(response));
    }

    /// <summary>
    /// 查詢單則訊息狀態
    /// </summary>
    /// <remarks>
    /// GET /api/v1/messages/{id}
    /// 
    /// 認證方式：API Key 或 JWT Token
    /// </remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetMessage(int id)
    {
        // 此端點可由 API Key 或 JWT Token 存取
        var hasApiKey = HttpContext.Items["ApiKeyId"] != null;
        var hasJwt = User.Identity?.IsAuthenticated ?? false;

        if (!hasApiKey && !hasJwt)
        {
            throw new UnauthorizedException();
        }

        var message = await _messageDispatchService.GetMessageByIdAsync(id);

        if (message == null)
        {
            throw new NotFoundException("Message", id);
        }

        var response = new MessageResponse
        {
            Id = message.Id,
            MessageType = message.MessageType.Code,
            Title = message.Title,
            Content = message.Content,
            SourceHost = message.SourceHost,
            SourceService = message.SourceService,
            Status = message.ProcessedAt.HasValue ? "completed" : "processing",
            RecipientCount = message.Deliveries.Count,
            SentCount = message.Deliveries.Count(d => d.Status == Models.DeliveryStatus.Sent),
            FailedCount = message.Deliveries.Count(d => d.Status == Models.DeliveryStatus.Failed),
            CreatedAt = message.CreatedAt,
            CompletedAt = message.ProcessedAt
        };

        return Ok(ApiResponse<MessageResponse>.Ok(response));
    }

    /// <summary>
    /// 查詢訊息歷史（管理員）
    /// </summary>
    /// <remarks>
    /// GET /api/v1/messages
    /// 
    /// 認證方式：JWT Token（管理員）
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<MessageListResponse>), 200)]
    public async Task<IActionResult> GetMessages([FromQuery] MessageQueryRequest query)
    {
        var response = await _messageDispatchService.GetMessagesAsync(query);
        return Ok(ApiResponse<MessageListResponse>.Ok(response));
    }

    /// <summary>
    /// 查詢我的訊息（使用者）
    /// </summary>
    /// <remarks>
    /// GET /api/v1/messages/me
    /// 
    /// 認證方式：JWT Token（使用者）
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserMessageListResponse>), 200)]
    public async Task<IActionResult> GetMyMessages([FromQuery] MessageQueryRequest query)
    {
        var userType = User.FindFirst("user_type")?.Value;

        // 如果是管理員，需要有關聯的使用者帳號
        if (userType == "admin")
        {
            throw new BadRequestException(ErrorCodes.INVALID_REQUEST, "管理員請使用 GET /api/v1/messages 查詢所有訊息");
        }

        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(subClaim, out var userId))
        {
            throw new UnauthorizedException();
        }

        var response = await _messageDispatchService.GetUserMessagesAsync(userId, query);
        return Ok(ApiResponse<UserMessageListResponse>.Ok(response));
    }
}
