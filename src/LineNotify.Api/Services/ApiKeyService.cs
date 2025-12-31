using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LineNotify.Api.Data;
using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LineNotify.Api.Services;

/// <summary>
/// API Key 服務實作
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;
    private const string KeyPrefix = "lnk_"; // Line Notify Key 前綴
    private const int KeyLength = 32; // 金鑰長度（不含前綴）

    public ApiKeyService(AppDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>取得 API Key 列表（分頁）</summary>
    public async Task<ApiKeyListResponse> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null)
    {
        var query = _context.ApiKeys
            .Include(k => k.CreatedByAdmin)
            .AsQueryable();

        // 搜尋（名稱或描述）
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(k => 
                k.Name.ToLower().Contains(searchLower) || 
                (k.Description != null && k.Description.ToLower().Contains(searchLower)) ||
                k.KeyPrefix.ToLower().Contains(searchLower));
        }

        // 篩選啟用狀態
        if (isActive.HasValue)
        {
            query = query.Where(k => k.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new ApiKeyItem
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                KeyPrefix = k.KeyPrefix,
                IsActive = k.IsActive,
                ExpiresAt = k.ExpiresAt,
                IsExpired = k.ExpiresAt.HasValue && k.ExpiresAt < DateTime.UtcNow,
                LastUsedAt = k.LastUsedAt,
                UsageCount = k.UsageCount,
                CreatedByName = k.CreatedByAdmin.Username,
                CreatedAt = k.CreatedAt
            })
            .ToListAsync();

        return new ApiKeyListResponse
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount
            }
        };
    }

    /// <summary>取得 API Key 詳細資訊</summary>
    public async Task<ApiKeyDetailResponse?> GetByIdAsync(int id)
    {
        var apiKey = await _context.ApiKeys
            .Include(k => k.CreatedByAdmin)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (apiKey == null)
            return null;

        return MapToDetailResponse(apiKey);
    }

    /// <summary>建立 API Key（回傳完整金鑰，僅此一次）</summary>
    public async Task<CreateApiKeyResponse> CreateAsync(CreateApiKeyRequest request, int adminId)
    {
        // 產生隨機金鑰
        var rawKey = GenerateRandomKey();
        var fullKey = KeyPrefix + rawKey;
        var keyHash = ComputeHash(fullKey);
        var keyPrefixDisplay = fullKey.Substring(0, Math.Min(12, fullKey.Length)) + "...";

        // 計算過期時間
        DateTime? expiresAt = request.ExpiresInDays.HasValue
            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
            : null;

        // 建立權限設定 JSON
        string? permissionsJson = null;
        if (request.AllowedMessageTypeIds?.Any() == true || request.AllowedGroupIds?.Any() == true)
        {
            var permissions = new ApiKeyPermissions
            {
                AllowedMessageTypeIds = request.AllowedMessageTypeIds,
                AllowedGroupIds = request.AllowedGroupIds
            };
            permissionsJson = JsonSerializer.Serialize(permissions);
        }

        var apiKey = new ApiKey
        {
            CreatedByAdminId = adminId,
            Name = request.Name,
            Description = request.Description,
            KeyHash = keyHash,
            KeyPrefix = keyPrefixDisplay,
            Permissions = permissionsJson,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key 已建立: {KeyPrefix} by Admin {AdminId}", keyPrefixDisplay, adminId);

        return new CreateApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            ApiKey = fullKey, // 完整金鑰僅此一次顯示
            KeyPrefix = keyPrefixDisplay,
            ExpiresAt = expiresAt,
            CreatedAt = apiKey.CreatedAt
        };
    }

    /// <summary>更新 API Key</summary>
    public async Task<ApiKeyDetailResponse?> UpdateAsync(int id, UpdateApiKeyRequest request)
    {
        var apiKey = await _context.ApiKeys
            .Include(k => k.CreatedByAdmin)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (apiKey == null)
            return null;

        apiKey.Name = request.Name;
        apiKey.Description = request.Description;
        apiKey.IsActive = request.IsActive;
        apiKey.ExpiresAt = request.ExpiresAt;
        apiKey.UpdatedAt = DateTime.UtcNow;

        // 更新權限設定
        if (request.AllowedMessageTypeIds?.Any() == true || request.AllowedGroupIds?.Any() == true)
        {
            var permissions = new ApiKeyPermissions
            {
                AllowedMessageTypeIds = request.AllowedMessageTypeIds,
                AllowedGroupIds = request.AllowedGroupIds
            };
            apiKey.Permissions = JsonSerializer.Serialize(permissions);
        }
        else
        {
            apiKey.Permissions = null;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key 已更新: {KeyId}", id);

        return MapToDetailResponse(apiKey);
    }

    /// <summary>撤銷（停用）API Key</summary>
    public async Task<bool> RevokeAsync(int id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey == null)
            return false;

        apiKey.IsActive = false;
        apiKey.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key 已撤銷: {KeyId}", id);
        return true;
    }

    /// <summary>刪除 API Key</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey == null)
            return false;

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key 已刪除: {KeyId}", id);
        return true;
    }

    /// <summary>驗證 API Key（回傳 API Key 實體或 null）</summary>
    public async Task<ApiKey?> ValidateAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        // 檢查前綴
        if (!apiKey.StartsWith(KeyPrefix))
            return null;

        var keyHash = ComputeHash(apiKey);

        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (key == null)
            return null;

        // 檢查是否啟用
        if (!key.IsActive)
        {
            _logger.LogWarning("嘗試使用已停用的 API Key: {KeyPrefix}", key.KeyPrefix);
            return null;
        }

        // 檢查是否過期
        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("嘗試使用已過期的 API Key: {KeyPrefix}", key.KeyPrefix);
            return null;
        }

        return key;
    }

    /// <summary>檢查 API Key 是否有權限使用特定訊息類型</summary>
    public async Task<bool> HasPermissionForMessageTypeAsync(int apiKeyId, int messageTypeId)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null || string.IsNullOrWhiteSpace(apiKey.Permissions))
            return true; // 無權限設定表示不限制

        var permissions = JsonSerializer.Deserialize<ApiKeyPermissions>(apiKey.Permissions);
        if (permissions?.AllowedMessageTypeIds == null || !permissions.AllowedMessageTypeIds.Any())
            return true; // 無訊息類型限制

        return permissions.AllowedMessageTypeIds.Contains(messageTypeId);
    }

    /// <summary>檢查 API Key 是否有權限發送到特定群組</summary>
    public async Task<bool> HasPermissionForGroupAsync(int apiKeyId, int groupId)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null || string.IsNullOrWhiteSpace(apiKey.Permissions))
            return true; // 無權限設定表示不限制

        var permissions = JsonSerializer.Deserialize<ApiKeyPermissions>(apiKey.Permissions);
        if (permissions?.AllowedGroupIds == null || !permissions.AllowedGroupIds.Any())
            return true; // 無群組限制

        return permissions.AllowedGroupIds.Contains(groupId);
    }

    /// <summary>記錄 API Key 使用</summary>
    public async Task RecordUsageAsync(int apiKeyId)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            apiKey.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }

    #region Private Methods

    /// <summary>產生隨機金鑰</summary>
    private static string GenerateRandomKey()
    {
        var bytes = new byte[KeyLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, KeyLength);
    }

    /// <summary>計算 SHA-256 雜湊</summary>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>映射到詳細回應</summary>
    private static ApiKeyDetailResponse MapToDetailResponse(ApiKey apiKey)
    {
        ApiKeyPermissions? permissions = null;
        if (!string.IsNullOrWhiteSpace(apiKey.Permissions))
        {
            permissions = JsonSerializer.Deserialize<ApiKeyPermissions>(apiKey.Permissions);
        }

        return new ApiKeyDetailResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Description = apiKey.Description,
            KeyPrefix = apiKey.KeyPrefix,
            IsActive = apiKey.IsActive,
            ExpiresAt = apiKey.ExpiresAt,
            IsExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt < DateTime.UtcNow,
            LastUsedAt = apiKey.LastUsedAt,
            UsageCount = apiKey.UsageCount,
            CreatedByAdminId = apiKey.CreatedByAdminId,
            CreatedByName = apiKey.CreatedByAdmin.Username,
            CreatedAt = apiKey.CreatedAt,
            UpdatedAt = apiKey.UpdatedAt,
            Permissions = permissions
        };
    }

    #endregion
}
