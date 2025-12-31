using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 使用者管理服務介面
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 取得使用者列表（分頁）
    /// </summary>
    /// <param name="page">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="search">搜尋字串（名稱）</param>
    /// <param name="groupId">篩選群組</param>
    /// <param name="isActive">篩選狀態</param>
    /// <returns>使用者列表</returns>
    Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        int? groupId = null,
        bool? isActive = null);

    /// <summary>
    /// 取得單一使用者
    /// </summary>
    /// <param name="id">使用者 ID</param>
    /// <returns>使用者</returns>
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// 更新使用者
    /// </summary>
    /// <param name="id">使用者 ID</param>
    /// <param name="displayName">顯示名稱</param>
    /// <param name="isActive">是否啟用</param>
    /// <param name="isAdmin">是否為管理員</param>
    /// <returns>更新後的使用者</returns>
    Task<User?> UpdateUserAsync(int id, string? displayName, bool? isActive, bool? isAdmin);

    /// <summary>
    /// 停用使用者
    /// </summary>
    /// <param name="id">使用者 ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeactivateUserAsync(int id);

    /// <summary>
    /// 取得使用者總數
    /// </summary>
    /// <returns>使用者總數</returns>
    Task<int> GetUserCountAsync();

    /// <summary>
    /// 取得活躍使用者數（有群組的使用者）
    /// </summary>
    /// <returns>活躍使用者數</returns>
    Task<int> GetActiveUserCountAsync();
}
