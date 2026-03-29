using Microsoft.EntityFrameworkCore;
using RadRoofer.Core.Entities;
using RadRoofer.Core.Enums;

namespace RadRoofer.Infrastructure.Data.Configurations;

public static class SeedData
{
    public static readonly Guid SeedOrganizationId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid SeedLocationId = Guid.Parse("22222222-0000-0000-0000-000000000002");
    public static readonly Guid SeedUserId = Guid.Parse("33333333-0000-0000-0000-000000000003");

    public static void Seed(ModelBuilder modelBuilder)
    {
        var seedTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<Organization>().HasData(new Organization
        {
            Id = SeedOrganizationId,
            Name = "Demo Roofing Co",
            Status = OrganizationStatus.Active,
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        });

        modelBuilder.Entity<ServiceLocation>().HasData(new
        {
            Id = SeedLocationId,
            OrganizationId = SeedOrganizationId,
            Name = "Main Office",
            Timezone = "America/Chicago",
            Status = ServiceLocationStatus.Open,
            VapiEnabled = true,
            VapiSecret = "dev-vapi-secret",
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        });

        // Password: Admin123! — hashed with BCrypt cost 11
        modelBuilder.Entity<AppUser>().HasData(new
        {
            Id = SeedUserId,
            OrganizationId = SeedOrganizationId,
            Email = "admin@roofers.tech",
            PasswordHash = "$2a$11$bMX0Dbive85wi3hnMEP/EeYS1Mb9EFHj/JiVzBlz6vrcek02.4M86",
            Role = UserRole.Owner,
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        });
    }
}
