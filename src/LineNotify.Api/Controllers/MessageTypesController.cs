using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineNotify.Api.Controllers;

/// <summary>
/// 訊息類型管理 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/admin/message-types")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class MessageTypesController : ControllerBase
{
    private readonly IMessageTypeService _messageTypeService;
    private readonly ILogger<MessageTypesController> _logger;

    public MessageTypesController(IMessageTypeService messageTypeService, ILogger<MessageTypesController> logger)
    {
        _messageTypeService = messageTypeService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有訊息類型
    /// </summary>
    /// <returns>訊息類型列表</returns>
    /// <response code="200">成功取得訊息類型列表</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MessageTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessageTypes()
    {
        var messageTypes = await _messageTypeService.GetMessageTypesAsync();
        return Ok(ApiResponse<List<MessageTypeResponse>>.Ok(messageTypes, "訊息類型列表取得成功"));
    }

    /// <summary>
    /// 取得指定訊息類型
    /// </summary>
    /// <param name="id">訊息類型 ID</param>
    /// <returns>訊息類型詳細資訊</returns>
    /// <response code="200">成功取得訊息類型</response>
    /// <response code="404">訊息類型不存在</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MessageTypeDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageType(int id)
    {
        var messageType = await _messageTypeService.GetMessageTypeByIdAsync(id);
        if (messageType == null)
        {
            throw new NotFoundException("MessageType", id);
        }

        var response = new MessageTypeDetailResponse
        {
            Id = messageType.Id,
            Code = messageType.Code,
            Name = messageType.Name,
            Description = messageType.Description,
            IsActive = messageType.IsActive,
            SubscriberCount = messageType.Subscriptions.Count(s => s.IsActive),
            GroupCount = messageType.GroupMessageTypes.Count,
            CreatedAt = messageType.CreatedAt,
            UpdatedAt = messageType.UpdatedAt
        };

        return Ok(ApiResponse<MessageTypeDetailResponse>.Ok(response));
    }

    /// <summary>
    /// 建立新訊息類型
    /// </summary>
    /// <param name="request">訊息類型建立請求</param>
    /// <returns>新建立的訊息類型</returns>
    /// <response code="201">訊息類型建立成功</response>
    /// <response code="400">請求格式錯誤</response>
    /// <response code="409">訊息類型代碼已存在</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageTypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMessageType([FromBody] CreateMessageTypeRequest request)
    {
        var messageType = await _messageTypeService.CreateMessageTypeAsync(request);

        var response = new MessageTypeResponse
        {
            Id = messageType.Id,
            Code = messageType.Code,
            Name = messageType.Name,
            Description = messageType.Description,
            IsActive = messageType.IsActive,
            CreatedAt = messageType.CreatedAt
        };

        _logger.LogInformation("管理員建立了新訊息類型: {Code}", messageType.Code);

        return CreatedAtAction(
            nameof(GetMessageType),
            new { id = messageType.Id },
            ApiResponse<MessageTypeResponse>.Ok(response, "訊息類型建立成功"));
    }

    /// <summary>
    /// 更新訊息類型
    /// </summary>
    /// <param name="id">訊息類型 ID</param>
    /// <param name="request">訊息類型更新請求</param>
    /// <returns>更新後的訊息類型</returns>
    /// <response code="200">訊息類型更新成功</response>
    /// <response code="404">訊息類型不存在</response>
    /// <response code="409">訊息類型代碼已被使用</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MessageTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMessageType(int id, [FromBody] UpdateMessageTypeRequest request)
    {
        var messageType = await _messageTypeService.UpdateMessageTypeAsync(id, request);

        var response = new MessageTypeResponse
        {
            Id = messageType.Id,
            Code = messageType.Code,
            Name = messageType.Name,
            Description = messageType.Description,
            IsActive = messageType.IsActive,
            CreatedAt = messageType.CreatedAt
        };

        _logger.LogInformation("管理員更新了訊息類型: {Id}", id);

        return Ok(ApiResponse<MessageTypeResponse>.Ok(response, "訊息類型更新成功"));
    }

    /// <summary>
    /// 刪除訊息類型
    /// </summary>
    /// <param name="id">訊息類型 ID</param>
    /// <returns>無內容</returns>
    /// <response code="204">訊息類型刪除成功</response>
    /// <response code="404">訊息類型不存在</response>
    /// <response code="409">訊息類型無法刪除（有關聯的訊息或訂閱）</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMessageType(int id)
    {
        await _messageTypeService.DeleteMessageTypeAsync(id);

        _logger.LogInformation("管理員刪除了訊息類型: {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// 取得訊息類型的訂閱者
    /// </summary>
    /// <param name="id">訊息類型 ID</param>
    /// <returns>訂閱者列表</returns>
    /// <response code="200">成功取得訂閱者列表</response>
    /// <response code="404">訊息類型不存在</response>
    [HttpGet("{id:int}/subscribers")]
    [ProducesResponseType(typeof(ApiResponse<List<SubscriberResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscribers(int id)
    {
        var subscribers = await _messageTypeService.GetSubscribersAsync(id);

        return Ok(ApiResponse<List<SubscriberResponse>>.Ok(subscribers, "訂閱者列表取得成功"));
    }
}

/// <summary>
/// 訊息類型詳細回應
/// </summary>
public class MessageTypeDetailResponse : MessageTypeResponse
{
    /// <summary>訂閱者數量</summary>
    public int SubscriberCount { get; set; }

    /// <summary>關聯群組數量</summary>
    public int GroupCount { get; set; }

    /// <summary>更新時間</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 訂閱者回應
/// </summary>
public class SubscriberResponse
{
    /// <summary>使用者 ID</summary>
    public int UserId { get; set; }

    /// <summary>顯示名稱</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>訂閱篩選條件</summary>
    public string? Filter { get; set; }

    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }

    /// <summary>訂閱來源（直接訂閱或透過群組）</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>訂閱時間</summary>
    public DateTime SubscribedAt { get; set; }
}
