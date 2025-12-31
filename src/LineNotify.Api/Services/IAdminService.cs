using LineNotify.Api.DTOs.Requests;
using LineNotify.Api.DTOs.Responses;
using LineNotify.Api.Models;

namespace LineNotify.Api.Services;

/// <summary>
/// 管理員服務介面
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// 驗證管理員登入
    /// </summary>
    /// <param name="username">帳號</param>
    /// <param name="password">密碼</param>
    /// <returns>管理員資訊，若驗證失敗則為 null</returns>
    Task<Admin?> ValidateLoginAsync(string username, string password);

    /// <summary>
    /// 修改管理員密碼
    /// </summary>
    /// <param name="adminId">管理員 ID</param>
    /// <param name="currentPassword">目前密碼</param>
    /// <param name="newPassword">新密碼</param>
    /// <returns>是否成功</returns>
    Task<bool> ChangePasswordAsync(int adminId, string currentPassword, string newPassword);

    /// <summary>
    /// 取得管理員列表
    /// </summary>
    Task<List<AdminListResponse>> GetAdminsAsync();

    /// <summary>
    /// 取得管理員詳細資訊
    /// </summary>
    Task<Admin?> GetAdminByIdAsync(int id);

    /// <summary>
    /// 建立管理員
    /// </summary>
    Task<Admin> CreateAdminAsync(CreateAdminRequest request);

    /// <summary>
    /// 更新管理員
    /// </summary>
    Task<Admin> UpdateAdminAsync(int id, UpdateAdminRequest request);

    /// <summary>
    /// 刪除管理員
    /// </summary>
    Task DeleteAdminAsync(int id, int currentAdminId);

    /// <summary>
    /// 重設管理員密碼
    /// </summary>
    Task<string> ResetPasswordAsync(int adminId, int currentAdminId);

    /// <summary>
    /// 檢查帳號是否存在
    /// </summary>
    Task<bool> IsUsernameExistsAsync(string username, int? excludeId = null);
}
