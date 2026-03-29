using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RadRoofer.Core.Entities;

namespace RadRoofer.Api.Pages;

[Authorize]
public class DashboardModel(AppDbContext db) : PageModel
{
    public List<Customer> Customers { get; private set; } = [];
    public List<CallLog> Calls { get; private set; } = [];
    public List<ServiceLocation> Locations { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Locations = await db.ServiceLocations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

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

    public async Task<IActionResult> OnPostToggleAssistantAsync(Guid locationId, CancellationToken ct)
    {
        ServiceLocation? location = await db.ServiceLocations
            .FirstOrDefaultAsync(o => o.Id == locationId, ct);

        if (location is null) return NotFound();

        location.VapiEnabled = !location.VapiEnabled;
        await db.SaveChangesAsync(ct);

        return RedirectToPage();
    }
}
