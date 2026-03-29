using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class AppUser : BaseEntity, IOrganizationIsolated
{
    [MaxLength(200)]
    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(200)]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Owner;

    public Organization Organization { get; set; } = null!;
}
