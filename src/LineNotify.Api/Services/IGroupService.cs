using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 群組服務介面
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// 取得所有群組（分頁）
    /// </summary>
    Task<PagedResponse<GroupListItemResponse>> GetGroupsAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);

    /// <summary>
    /// 取得所有群組列表
    /// </summary>
    Task<List<GroupResponse>> GetAllGroupsAsync();

    /// <summary>
    /// 取得單一群組詳細資訊
    /// </summary>
    Task<GroupResponse?> GetGroupByIdAsync(int id);

    /// <summary>
    /// 取得群組實體（含相關資料）
    /// </summary>
    Task<Group?> GetGroupEntityByIdAsync(int id);

    /// <summary>
    /// 以代碼取得群組
    /// </summary>
    Task<GroupResponse?> GetGroupByCodeAsync(string code);

    /// <summary>
    /// 建立群組
    /// </summary>
    Task<Group> CreateGroupAsync(CreateGroupRequest request);

    /// <summary>
    /// 更新群組
    /// </summary>
    Task<Group> UpdateGroupAsync(int id, UpdateGroupRequest request);

    /// <summary>
    /// 刪除群組
    /// </summary>
    Task DeleteGroupAsync(int id);

    /// <summary>
    /// 取得群組成員列表
    /// </summary>
    Task<List<GroupMemberResponse>> GetGroupMembersAsync(int groupId);

    /// <summary>
    /// 批次加入群組成員
    /// </summary>
    Task<int> AddGroupMembersAsync(int groupId, List<int> userIds);

    /// <summary>
    /// 批次移除群組成員
    /// </summary>
    Task<int> RemoveGroupMembersAsync(int groupId, List<int> userIds);

    /// <summary>
    /// 設定群組的訊息類型
    /// </summary>
    Task SetGroupMessageTypesAsync(int groupId, List<int> messageTypeIds);

    /// <summary>
    /// 檢查群組代碼是否存在
    /// </summary>
    Task<bool> IsCodeExistsAsync(string code, int? excludeId = null);
}
