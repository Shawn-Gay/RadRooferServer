using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RadRoofer.Api.Filters;
using RadRoofer.Api.Services;
using RadRoofer.Api.Settings;
using RadRoofer.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly("RadRoofer.Infrastructure")));

builder.Services.AddHttpContextAccessor();

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
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(
            "http://localhost:8081",
            "http://localhost:8082",
            "http://localhost:8083",
            "http://localhost:8084",
            "http://localhost:8085",
            "http://localhost:8086")
     .AllowAnyHeader()
     .AllowAnyMethod()));
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services.AddScoped<VapiSecretAuthFilter>();
builder.Services.Configure<GoogleCalendarSettings>(
    builder.Configuration.GetSection("GoogleCalendar"));
builder.Services.AddScoped<GoogleCalendarService>();
builder.Services.Configure<VapiSettings>(
    builder.Configuration.GetSection("Vapi"));
builder.Services.AddHttpClient("vapi", (sp, client) =>
{
    var apiKey = sp.GetRequiredService<IOptions<VapiSettings>>().Value.ApiKey;
    client.BaseAddress = new Uri("https://api.vapi.ai/");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
