using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.StartTime).HasColumnType("timestamp with time zone");
        builder.Property(o => o.EndTime).HasColumnType("timestamp with time zone");
        builder.Property(o => o.LastSynced).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey("OrganizationId");

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.Appointments)
               .HasForeignKey("ServiceLocationId");

        builder.HasOne(o => o.Customer)
               .WithMany(o => o.Appointments)
               .HasForeignKey("CustomerId")
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Employee)
               .WithMany(o => o.Appointments)
               .HasForeignKey("EmployeeId")
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Service)
               .WithMany(o => o.Appointments)
               .HasForeignKey("ServiceId")
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex("OrganizationId");
        builder.HasIndex("ServiceLocationId");
        builder.HasIndex("CustomerId");
    }
}
