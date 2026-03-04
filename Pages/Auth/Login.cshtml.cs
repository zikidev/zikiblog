using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ZikiBlog.Pages.Auth;

public class LoginModel : PageModel
{
    public bool HasError { get; set; }

    public void OnGet()
    {
        HasError = Request.Query.ContainsKey("error");
    }
}
