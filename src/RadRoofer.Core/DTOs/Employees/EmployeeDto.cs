namespace RadRoofer.Core.DTOs.Employees;

public record EmployeeDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Role { get; init; }
    public bool IsBookable { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
