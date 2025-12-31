namespace LineNotify.Api.DTOs.Responses;

/// <summary>
/// API Key 列表回應
/// </summary>
public class ApiKeyListResponse
{
    /// <summary>API Key 列表</summary>
    public List<ApiKeyItem> Items { get; set; } = new();

    /// <summary>分頁資訊</summary>
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// API Key 列表項目
/// </summary>
public class ApiKeyItem
{
    /// <summary>ID</summary>
    public int Id { get; set; }

    /// <summary>名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>金鑰前綴（用於識別，如 "lnk_abc12..."）</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }

    /// <summary>過期時間</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>是否已過期</summary>
    public bool IsExpired { get; set; }

    /// <summary>最後使用時間</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>使用次數</summary>
    public long UsageCount { get; set; }

    /// <summary>建立者名稱</summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// API Key 詳細資訊回應
/// </summary>
public class ApiKeyDetailResponse
{
    /// <summary>ID</summary>
    public int Id { get; set; }

    /// <summary>名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>金鑰前綴</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }

    /// <summary>過期時間</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>是否已過期</summary>
    public bool IsExpired { get; set; }

    /// <summary>最後使用時間</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>使用次數</summary>
    public long UsageCount { get; set; }

    /// <summary>建立者 ID</summary>
    public int CreatedByAdminId { get; set; }

    /// <summary>建立者名稱</summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>權限設定</summary>
    public ApiKeyPermissions? Permissions { get; set; }
}

/// <summary>
/// API Key 權限設定
/// </summary>
public class ApiKeyPermissions
{
    /// <summary>允許的訊息類型 ID 清單</summary>
    public List<int>? AllowedMessageTypeIds { get; set; }

    /// <summary>允許的群組 ID 清單</summary>
    public List<int>? AllowedGroupIds { get; set; }
}

/// <summary>
/// 建立 API Key 回應（包含完整金鑰，僅在建立時顯示一次）
/// </summary>
public class CreateApiKeyResponse
{
    /// <summary>ID</summary>
    public int Id { get; set; }

    /// <summary>名稱</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>完整金鑰（僅在建立時顯示一次，請妥善保存）</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>金鑰前綴（用於識別）</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>過期時間</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>警告訊息</summary>
    public string Warning { get; set; } = "請立即複製並妥善保存此金鑰，此金鑰將不會再次顯示。";
}
