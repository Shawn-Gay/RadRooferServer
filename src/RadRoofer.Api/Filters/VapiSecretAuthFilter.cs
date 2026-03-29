using System.Security.Cryptography;

namespace RadRoofer.Api.Filters;

public class VapiSecretAuthFilter(AppDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (!ctx.RouteData.Values.TryGetValue("locationId", out var rawId)
            || !Guid.TryParse(rawId?.ToString(), out var locationId))
        {
            ctx.Result = new BadRequestResult();
            return;
        }

        var incomingSecret = ctx.HttpContext.Request.Headers["X-Vapi-Secret"].ToString();

        var location = await db.ServiceLocations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == locationId);

        if (location is null || string.IsNullOrEmpty(location.VapiSecret)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(incomingSecret),
                Encoding.UTF8.GetBytes(location.VapiSecret)))
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
