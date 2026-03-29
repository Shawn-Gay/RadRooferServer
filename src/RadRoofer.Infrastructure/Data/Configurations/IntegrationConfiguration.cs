using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Provider).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.ConfigJson).HasColumnType("jsonb");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.Integrations)
               .HasForeignKey("ServiceLocationId");

        builder.HasIndex("OrganizationId");
        builder.HasIndex("ServiceLocationId");
    }
}
