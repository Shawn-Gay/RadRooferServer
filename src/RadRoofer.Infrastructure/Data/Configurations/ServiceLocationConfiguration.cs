using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;
using RadRoofer.Core.Enums;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class ServiceLocationConfiguration : IEntityTypeConfiguration<ServiceLocation>
{
    public void Configure(EntityTypeBuilder<ServiceLocation> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.VapiEnabled).HasDefaultValue(true);
        builder.Property(o => o.VapiSecret).HasMaxLength(200);
        builder.Property(o => o.VapiAssistantId).HasMaxLength(200);
        builder.Property(o => o.VapiPhoneNumberId).HasMaxLength(200);
        builder.Property(o => o.CalendarId).HasMaxLength(200);
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany(o => o.ServiceLocations)
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.Details)
               .WithOne(o => o.ServiceLocation)
               .HasForeignKey<ServiceLocationDetails>("ServiceLocationId");

        builder.HasMany(o => o.ContactInfos)
               .WithMany()
               .UsingEntity("ServiceLocationContactInfo");

        builder.HasIndex("OrganizationId");
    }
}
