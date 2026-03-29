using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;
using RadRoofer.Core.Enums;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Details)
               .WithOne(o => o.Organization)
               .HasForeignKey<OrganizationDetails>("OrganizationId");
    }
}
