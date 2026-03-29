namespace RadRoofer.Core.DTOs.Auth;

public record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string Role,
    Guid TenantId);
