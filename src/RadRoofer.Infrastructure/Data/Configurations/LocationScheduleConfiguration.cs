using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadRoofer.Core.Entities;

namespace RadRoofer.Infrastructure.Data.Configurations;

public class LocationScheduleConfiguration : IEntityTypeConfiguration<LocationSchedule>
{
    public void Configure(EntityTypeBuilder<LocationSchedule> builder)
    {
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.DayOfWeek).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.WorkStart).HasColumnType("time without time zone");
        builder.Property(o => o.WorkEnd).HasColumnType("time without time zone");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(o => o.SoftDeletedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey(o => o.OrganizationId);

        builder.HasOne(o => o.ServiceLocation)
               .WithMany(o => o.Schedule)
               .HasForeignKey(o => o.ServiceLocationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.OrganizationId);
        builder.HasIndex(o => new { o.ServiceLocationId, o.DayOfWeek }).IsUnique();
    }
}
