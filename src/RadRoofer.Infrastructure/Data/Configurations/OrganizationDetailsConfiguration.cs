using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;
using RadRoofer.Core.Enums;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class OrganizationDetailsConfiguration : IEntityTypeConfiguration<OrganizationDetails>
{
    public void Configure(EntityTypeBuilder<OrganizationDetails> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.PrimaryIndustry).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex("OrganizationId").IsUnique();
    }
}
