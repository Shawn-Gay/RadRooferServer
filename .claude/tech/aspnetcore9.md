# ASP.NET Core 9 — Patterns Reference (LLM)

## Controller Skeleton — this project uses controllers, NOT minimal API
```csharp
[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class LeadsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadDto>>> GetLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] LeadStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.Leads.AsNoTracking();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<LeadDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            Total = total, Page = page, PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDto>> GetLead(Guid id, CancellationToken ct)
    {
        var lead = await db.Leads.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        return lead is null ? NotFound() : Ok(lead.ToDto());
    }
}
```

---

## Dual Auth — JWT for /v1/*, Cookie for Razor Pages
```csharp
// Program.cs
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme = "SmartScheme";
        o.DefaultChallengeScheme = "SmartScheme";
    })
    .AddPolicyScheme("SmartScheme", displayName: null, o =>
    {
        o.ForwardDefaultSelector = ctx =>
            ctx.Request.Path.StartsWithSegments("/v1")
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddCookie(o => { o.LoginPath = "/login"; o.SlidingExpiration = true; });

// JWT generation (AuthController)
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim("tenant_id", user.TenantId.ToString()),
    new Claim("role", user.Role.ToString()),
    new Claim(JwtRegisteredClaimNames.Email, user.Email)
};
var token = new JwtSecurityToken(
    claims: claims,
    expires: DateTime.UtcNow.AddDays(7),
    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
```

---

## VapiSecretAuthFilter — action filter, not middleware
```csharp
// Runs per-action; IgnoreQueryFilters because tenant not yet resolved
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

        var location = await db.Locations
            .IgnoreQueryFilters()  // tenant not resolved yet — must bypass filter
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == locationId);

        if (location is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(incomingSecret),
                Encoding.UTF8.GetBytes(location.VapiSecret)))
        {
            ctx.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}

// Register in Program.cs
builder.Services.AddScoped<VapiSecretAuthFilter>();

// Apply to controller
[ServiceFilter<VapiSecretAuthFilter>]
[AllowAnonymous]  // webhook has no JWT — secret IS the auth
public class VapiWebhookController(AppDbContext db) : ControllerBase { }
```

---

## Problem Details — standard error response format
```csharp
// Program.cs — wire up once
builder.Services.AddProblemDetails();
app.UseExceptionHandler();  // catches unhandled exceptions

// Custom handler for domain exceptions
public class AppExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            KeyNotFoundException => (404, "Not Found"),
            UnauthorizedAccessException => (403, "Forbidden"),
            _ => (0, null)
        };
        if (status == 0) return false;

        ctx.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = { Title = title, Status = status },
            Exception = ex
        });
    }
}

// Register
builder.Services.AddExceptionHandler<AppExceptionHandler>();
```

---

## Request Validation — [ApiController] auto-validates
```csharp
// DataAnnotations on records
public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password);

// [ApiController] returns 400 ProblemDetails automatically on invalid model
// No need to check ModelState.IsValid manually
```

---

## Finbuckle Multi-Tenancy Setup
```csharp
// Program.cs — register before auth
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("tenant_id")              // resolves from JWT/cookie claim
    .WithStore<LocationTenantStore>(lifetime: ServiceLifetime.Scoped);  // custom for webhooks

// Custom store resolves locationId → TenantInfo for webhook routes
public class LocationTenantStore(AppDbContext db) : IMultiTenantStore<TenantInfo>
{
    public async Task<TenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        if (!Guid.TryParse(identifier, out var locationId)) return null;
        var location = await db.Locations.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == locationId);
        if (location is null) return null;
        return new TenantInfo { Id = location.TenantId.ToString(), Identifier = identifier };
    }
    // other interface methods return null/false — only identifier lookup is used
}

// Access tenant in controller/service
public class SomeHandler(IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
{
    private Guid TenantId => Guid.Parse(tenantAccessor.MultiTenantContext!.TenantInfo!.Id);
}
```

---

## Program.cs — Minimal Phase 0 Wiring Order
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddMultiTenant<TenantInfo>()...;
builder.Services.AddAuthentication(...)...;  // dual scheme
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddScoped<VapiSecretAuthFilter>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMultiTenant();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();
```

---

## Common Mistakes
- Don't return `IActionResult` when `ActionResult<T>` gives better OpenAPI inference
- Don't use `[FromBody]` — `[ApiController]` infers it automatically for complex types
- Don't use `app.UseRouting()` + `app.UseEndpoints()` — use `app.MapControllers()` directly
- Don't inject scoped services via constructor into `IAsyncActionFilter` — use `ServiceFilter<T>`
- Don't read `HttpContext` in constructors — it's request-scoped, only valid in methods
- Don't catch exceptions in controllers — use `IExceptionHandler`
- Don't configure `UseMultiTenant()` after `UseAuthentication()` — it must come before
- Don't use `[Authorize(AuthenticationSchemes = "...")]` on individual controllers — let PolicyScheme route by path
