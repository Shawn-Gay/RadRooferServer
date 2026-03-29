using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RadRoofer.Api.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Dashboard")
            : RedirectToPage("/Login");
}
