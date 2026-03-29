using Microsoft.AspNetCore.Http;
using RadRoofer.Core.Entities;
using RadRoofer.Infrastructure.Data.Configurations;

namespace RadRoofer.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : DbContext(options)
{
    private Guid? OrganizationId
    {
        get
        {
            var claim = httpContextAccessor?.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationDetails> OrganizationDetails => Set<OrganizationDetails>();
    public DbSet<ServiceLocation> ServiceLocations => Set<ServiceLocation>();
    public DbSet<ServiceLocationDetails> ServiceLocationDetails => Set<ServiceLocationDetails>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PhysicalLocation> PhysicalLocations => Set<PhysicalLocation>();
    public DbSet<ContactInfo> ContactInfos => Set<ContactInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<OrganizationDetails>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<ServiceLocation>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<ServiceLocationDetails>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<AppUser>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<Employee>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<Service>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<Integration>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<Customer>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault()
              && o.SoftDeletedAt == null);

        modelBuilder.Entity<CallLog>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<Appointment>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<PhysicalLocation>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        modelBuilder.Entity<ContactInfo>().HasQueryFilter(
            o => OrganizationId.HasValue && EF.Property<Guid>(o, "OrganizationId") == OrganizationId.GetValueOrDefault());

        SeedData.Seed(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable)
            {
                entry.State = EntityState.Modified;
                entry.Entity.SoftDeletedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;

                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
