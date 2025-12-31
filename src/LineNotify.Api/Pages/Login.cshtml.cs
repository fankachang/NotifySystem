using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LineNotify.Api.Pages;

/// <summary>
/// 登入頁面 Model
/// </summary>
public class LoginModel : PageModel
{
    public void OnGet()
    {
        // 頁面載入時不需特別處理
        // Line Login 流程由前端 JavaScript 處理
    }
}
