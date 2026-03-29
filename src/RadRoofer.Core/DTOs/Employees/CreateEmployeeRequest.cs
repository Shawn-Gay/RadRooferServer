using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Employees;

public record CreateEmployeeRequest
{
    public required Guid ServiceLocationId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public EmployeeRole Role { get; init; }
    public bool IsBookable { get; init; }
}
