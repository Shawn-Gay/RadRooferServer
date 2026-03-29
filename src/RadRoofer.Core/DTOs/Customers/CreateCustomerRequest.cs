using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Customers;

public record CreateCustomerRequest
{
    public required Guid ServiceLocationId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? CompanyName { get; init; }
    public LeadSource LeadSource { get; init; }
    public CustomerType CustomerType { get; init; }
    public string? ExternalId { get; init; }
}
