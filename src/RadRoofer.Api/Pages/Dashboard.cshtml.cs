using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RadRoofer.Core.Entities;

namespace RadRoofer.Api.Pages;

[Authorize]
public class DashboardModel(AppDbContext db) : PageModel
{
    public List<Customer> Customers { get; private set; } = [];
    public List<CallLog> Calls { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Customers = await db.Customers
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        Calls = await db.CallLogs
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
    }
}
