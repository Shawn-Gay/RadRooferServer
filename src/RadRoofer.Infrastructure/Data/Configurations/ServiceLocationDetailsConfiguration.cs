using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class ServiceLocationDetailsConfiguration : IEntityTypeConfiguration<ServiceLocationDetails>
{
    public void Configure(EntityTypeBuilder<ServiceLocationDetails> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasIndex("ServiceLocationId").IsUnique();
        builder.HasIndex("OrganizationId");
    }
}
