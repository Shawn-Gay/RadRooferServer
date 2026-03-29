using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
{
    public void Configure(EntityTypeBuilder<CallLog> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.EndedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.CallLogs)
               .HasForeignKey("ServiceLocationId");

        builder.HasOne(o => o.Customer)
               .WithMany(o => o.CallLogs)
               .HasForeignKey("CustomerId")
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Appointment)
               .WithMany()
               .HasForeignKey("AppointmentId")
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(o => o.VapiCallId).IsUnique();
        builder.HasIndex("OrganizationId");
        builder.HasIndex("ServiceLocationId");
    }
}
