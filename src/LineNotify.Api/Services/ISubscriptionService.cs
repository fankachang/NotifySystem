namespace LineNotify.Api.Services;

/// <summary>
/// 訂閱同步服務介面
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// 同步使用者在特定群組的訂閱
    /// 當使用者被加入群組時，自動建立該群組所有訊息類型的訂閱
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="groupId">群組 ID</param>
    Task SyncUserSubscriptionsAsync(int userId, int groupId);

    /// <summary>
    /// 同步群組所有成員的訂閱
    /// 當群組的訊息類型變更時，更新所有成員的訂閱
    /// </summary>
    /// <param name="groupId">群組 ID</param>
    Task SyncGroupSubscriptionsAsync(int groupId);

    /// <summary>
    /// 移除使用者在特定群組的所有訂閱
    /// 當使用者被移出群組時呼叫
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="groupId">群組 ID</param>
    Task RemoveUserGroupSubscriptionsAsync(int userId, int groupId);

    /// <summary>
    /// 取得使用者的所有有效訂閱
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>訂閱列表</returns>
    Task<List<Models.Subscription>> GetUserSubscriptionsAsync(int userId);

    /// <summary>
    /// 取得特定訊息類型的所有有效訂閱者
    /// </summary>
    /// <param name="messageTypeId">訊息類型 ID</param>
    /// <param name="sourceHost">來源主機（用於篩選）</param>
    /// <param name="sourceService">來源服務（用於篩選）</param>
    /// <returns>訂閱者使用者 ID 列表</returns>
    Task<List<int>> GetSubscribersAsync(int messageTypeId, string? sourceHost = null, string? sourceService = null);
}
