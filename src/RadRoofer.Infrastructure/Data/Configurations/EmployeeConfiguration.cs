using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.Employees)
               .HasForeignKey("ServiceLocationId");

        builder.HasMany(o => o.ContactInfos)
               .WithMany()
               .UsingEntity("EmployeeContactInfo");

        builder.HasIndex("OrganizationId");
        builder.HasIndex("ServiceLocationId");
    }
}
