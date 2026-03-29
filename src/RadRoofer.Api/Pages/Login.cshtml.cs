using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RadRoofer.Api.Pages;

public class LoginModel(AppDbContext db) : PageModel
{
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        string email,
        string password,
        CancellationToken ct)
    {
        var user = await db.AppUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Email == email)
            .Select(o => new
            {
                o.Id,
                o.PasswordHash,
                o.Role,
                o.Email,
                OrganizationId = EF.Property<Guid>(o, "OrganizationId"),
            })
            .FirstOrDefaultAsync(ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            Error = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("tenant_id", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Dashboard");
    }
}
