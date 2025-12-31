using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LineNotify.Api.Pages;

/// <summary>
/// 使用者儀表板頁面 Model
/// </summary>
public class DashboardModel : PageModel
{
    public void OnGet()
    {
        // 頁面載入時不需特別處理
        // 使用者資訊由前端 JavaScript 透過 API 載入
    }
}
