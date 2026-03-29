using BCrypt.Net;
using RadRoofer.Core.DTOs.Auth;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous] 
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var user = await db.AppUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Email == request.Email)
            .Select(o => new
            {
                o.Id,
                o.Email,
                o.PasswordHash,
                o.Role,
                OrganizationId = EF.Property<Guid>(o, "OrganizationId"),
            })
            .FirstOrDefaultAsync(ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var jwtSecret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("tenant_id", user.OrganizationId.ToString()),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var userDto = new UserDto(user.Id, user.Email, user.Role.ToString(), user.OrganizationId);
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, userDto));
    }
}
