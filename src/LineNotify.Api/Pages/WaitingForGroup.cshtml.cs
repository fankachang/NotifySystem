using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LineNotify.Api.Pages;

/// <summary>
/// 等待分配群組頁面 Model
/// </summary>
public class WaitingForGroupModel : PageModel
{
    public void OnGet()
    {
        // 頁面載入時不需特別處理
        // 群組狀態檢查由前端 JavaScript 處理
    }
}
