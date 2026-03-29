using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class PhysicalLocationConfiguration : IEntityTypeConfiguration<PhysicalLocation>
{
    public void Configure(EntityTypeBuilder<PhysicalLocation> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.EntityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.LocationType).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasMany(o => o.Customers)
               .WithMany(o => o.PhysicalLocations)
               .UsingEntity("CustomerPhysicalLocation");

        builder.HasMany(o => o.ServiceLocations)
               .WithMany(o => o.PhysicalLocations)
               .UsingEntity("ServiceLocationPhysicalLocation");

        builder.HasMany(o => o.Employees)
               .WithMany(o => o.PhysicalLocations)
               .UsingEntity("EmployeePhysicalLocation");

        builder.HasIndex(o => new { o.EntityType, o.EntityId });
        builder.HasIndex("OrganizationId");
    }
}
