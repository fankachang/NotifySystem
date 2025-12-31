using LineNotify.Api.Controllers;
using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Exceptions;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// 訊息類型服務實作
/// </summary>
public class MessageTypeService : IMessageTypeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MessageTypeService> _logger;

    public MessageTypeService(AppDbContext dbContext, ILogger<MessageTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<MessageTypeResponse>> GetMessageTypesAsync()
    {
        return await _dbContext.MessageTypes
            .OrderBy(mt => mt.Code)
            .Select(mt => new MessageTypeResponse
            {
                Id = mt.Id,
                Code = mt.Code,
                Name = mt.Name,
                Description = mt.Description,
                IsActive = mt.IsActive,
                CreatedAt = mt.CreatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<MessageType?> GetMessageTypeByIdAsync(int id)
    {
        return await _dbContext.MessageTypes
            .Include(mt => mt.Subscriptions)
                .ThenInclude(s => s.User)
            .Include(mt => mt.GroupMessageTypes)
            .FirstOrDefaultAsync(mt => mt.Id == id);
    }

    /// <inheritdoc/>
    public async Task<MessageType?> GetMessageTypeByCodeAsync(string code)
    {
        return await _dbContext.MessageTypes
            .FirstOrDefaultAsync(mt => mt.Code == code.ToUpper());
    }

    /// <inheritdoc/>
    public async Task<MessageType> CreateMessageTypeAsync(CreateMessageTypeRequest request)
    {
        // 檢查代碼是否已存在
        if (await IsCodeExistsAsync(request.Code))
        {
            throw new ConflictException(ErrorCodes.ALREADY_EXISTS, $"訊息類型代碼 '{request.Code}' 已存在");
        }

        var messageType = new MessageType
        {
            Code = request.Code.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.MessageTypes.Add(messageType);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("訊息類型已建立: {Code}", messageType.Code);

        return messageType;
    }

    /// <inheritdoc/>
    public async Task<MessageType> UpdateMessageTypeAsync(int id, UpdateMessageTypeRequest request)
    {
        var messageType = await _dbContext.MessageTypes.FindAsync(id);
        if (messageType == null)
        {
            throw new NotFoundException("MessageType", id);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            messageType.Name = request.Name;

        if (request.Description != null)
            messageType.Description = request.Description;

        if (request.IsActive.HasValue)
            messageType.IsActive = request.IsActive.Value;

        messageType.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("訊息類型已更新: {Id}", id);

        return messageType;
    }

    /// <inheritdoc/>
    public async Task DeleteMessageTypeAsync(int id)
    {
        var messageType = await _dbContext.MessageTypes
            .Include(mt => mt.Subscriptions)
            .Include(mt => mt.Messages)
            .FirstOrDefaultAsync(mt => mt.Id == id);

        if (messageType == null)
        {
            throw new NotFoundException("MessageType", id);
        }

        // 檢查是否有相關訊息
        if (messageType.Messages.Count != 0)
        {
            throw new ConflictException(
                ErrorCodes.CANNOT_DELETE,
                $"訊息類型 '{messageType.Code}' 有 {messageType.Messages.Count} 則相關訊息，無法刪除");
        }

        // 先移除相關的訂閱和群組關聯
        _dbContext.Subscriptions.RemoveRange(messageType.Subscriptions);

        var groupMessageTypes = await _dbContext.GroupMessageTypes
            .Where(gmt => gmt.MessageTypeId == id)
            .ToListAsync();
        _dbContext.GroupMessageTypes.RemoveRange(groupMessageTypes);

        _dbContext.MessageTypes.Remove(messageType);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("訊息類型已刪除: {Code}", messageType.Code);
    }

    /// <inheritdoc/>
    public async Task<List<SubscriberResponse>> GetSubscribersAsync(int messageTypeId)
    {
        var messageType = await _dbContext.MessageTypes.FindAsync(messageTypeId);
        if (messageType == null)
        {
            throw new NotFoundException("MessageType", messageTypeId);
        }

        // 取得所有訂閱（透過群組）
        var subscribers = await _dbContext.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Group)
            .Where(s => s.MessageTypeId == messageTypeId && s.IsActive)
            .Select(s => new SubscriberResponse
            {
                UserId = s.UserId,
                DisplayName = s.User.DisplayName,
                Filter = null,
                IsActive = s.IsActive,
                Source = $"群組: {s.Group.Name}",
                SubscribedAt = s.CreatedAt
            })
            .ToListAsync();

        // 去除重複（同一使用者可能在多個群組中）
        var uniqueSubscribers = subscribers
            .GroupBy(s => s.UserId)
            .Select(g => g.First())
            .OrderBy(s => s.DisplayName)
            .ToList();

        return uniqueSubscribers;
    }

    /// <inheritdoc/>
    public async Task<bool> IsCodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _dbContext.MessageTypes.Where(mt => mt.Code == code.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(mt => mt.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
