using LineNotify.Api.Controllers;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 訊息類型服務介面
/// </summary>
public interface IMessageTypeService
{
    /// <summary>
    /// 取得所有訊息類型
    /// </summary>
    Task<List<MessageTypeResponse>> GetMessageTypesAsync();

    /// <summary>
    /// 依 ID 取得訊息類型
    /// </summary>
    Task<MessageType?> GetMessageTypeByIdAsync(int id);

    /// <summary>
    /// 依代碼取得訊息類型
    /// </summary>
    Task<MessageType?> GetMessageTypeByCodeAsync(string code);

    /// <summary>
    /// 建立訊息類型
    /// </summary>
    Task<MessageType> CreateMessageTypeAsync(CreateMessageTypeRequest request);

    /// <summary>
    /// 更新訊息類型
    /// </summary>
    Task<MessageType> UpdateMessageTypeAsync(int id, UpdateMessageTypeRequest request);

    /// <summary>
    /// 刪除訊息類型
    /// </summary>
    Task DeleteMessageTypeAsync(int id);

    /// <summary>
    /// 取得訊息類型的訂閱者
    /// </summary>
    Task<List<SubscriberResponse>> GetSubscribersAsync(int messageTypeId);

    /// <summary>
    /// 檢查代碼是否存在
    /// </summary>
    Task<bool> IsCodeExistsAsync(string code, int? excludeId = null);
}
