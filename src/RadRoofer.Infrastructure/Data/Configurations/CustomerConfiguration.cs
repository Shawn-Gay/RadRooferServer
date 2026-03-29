using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.LeadSource).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CustomerType).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.LastSynced).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.SoftDeletedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.Customers)
               .HasForeignKey("ServiceLocationId");

        builder.HasMany(o => o.ContactInfos)
               .WithMany()
               .UsingEntity("CustomerContactInfo");

        builder.HasIndex("OrganizationId");
        builder.HasIndex("ServiceLocationId");
    }
}
