using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// API Key 服務介面
/// </summary>
public interface IApiKeyService
{
    /// <summary>取得 API Key 列表（分頁）</summary>
    Task<ApiKeyListResponse> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);

    /// <summary>取得 API Key 詳細資訊</summary>
    Task<ApiKeyDetailResponse?> GetByIdAsync(int id);

    /// <summary>建立 API Key（回傳完整金鑰，僅此一次）</summary>
    Task<CreateApiKeyResponse> CreateAsync(CreateApiKeyRequest request, int adminId);

    /// <summary>更新 API Key</summary>
    Task<ApiKeyDetailResponse?> UpdateAsync(int id, UpdateApiKeyRequest request);

    /// <summary>撤銷（停用）API Key</summary>
    Task<bool> RevokeAsync(int id);

    /// <summary>刪除 API Key</summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>驗證 API Key（回傳 API Key 實體或 null）</summary>
    Task<ApiKey?> ValidateAsync(string apiKey);

    /// <summary>檢查 API Key 是否有權限使用特定訊息類型</summary>
    Task<bool> HasPermissionForMessageTypeAsync(int apiKeyId, int messageTypeId);

    /// <summary>檢查 API Key 是否有權限發送到特定群組</summary>
    Task<bool> HasPermissionForGroupAsync(int apiKeyId, int groupId);

    /// <summary>記錄 API Key 使用</summary>
    Task RecordUsageAsync(int apiKeyId);
}
