namespace RadRoofer.Core.DTOs.Customers;

public record CustomerDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? CompanyName { get; init; }
    public required string LeadSource { get; init; }
    public required string CustomerType { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? LastSynced { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
