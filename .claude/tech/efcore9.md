# EF Core 9 + Npgsql — Patterns Reference (LLM)

## AppDbContext Skeleton
```csharp
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
    : DbContext(options)
{
    private Guid CurrentTenantId =>
        Guid.Parse(tenantAccessor.MultiTenantContext?.TenantInfo?.Id ?? Guid.Empty.ToString());

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<Lead> Leads => Set<Lead>();
    // ... all 11 entities

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> files in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters — stack TenantId + soft delete per entity
        modelBuilder.Entity<Lead>().HasQueryFilter(
            o => o.TenantId == CurrentTenantId && !o.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(
            o => o.TenantId == CurrentTenantId && !o.IsDeleted);
        modelBuilder.Entity<Job>().HasQueryFilter(
            o => o.TenantId == CurrentTenantId && !o.IsDeleted);
        // Entities without soft delete — tenant filter only
        modelBuilder.Entity<CallLog>().HasQueryFilter(o => o.TenantId == CurrentTenantId);
        modelBuilder.Entity<Location>().HasQueryFilter(o => o.TenantId == CurrentTenantId);
        // ... etc for all tenant-scoped entities
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)   entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        // Soft delete interception
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(o => o.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }
        return await base.SaveChangesAsync(ct);
    }
}
```

---

## Hybrid Approach — Annotations on entities, Fluent API for the rest

**Rule:** Put simple constraints on the entity with attributes. Keep `IEntityTypeConfiguration<T>` for things attributes *cannot* express: indexes, relationships, FK delete behavior, column types, and default values.

### Entity — use attributes for property constraints
```csharp
public class Lead
{
    public Guid Id { get; set; }                          // PK default set in config
    public required Guid TenantId { get; set; }
    public required Guid LocationId { get; set; }

    [MaxLength(200)] 
    public required string CallerName { get; set; }
    
    [MaxLength(30)]  
    public required string CallerPhone { get; set; }

    [MaxLength(500)] 

    public string? Address { get; set; }

    [MaxLength(50)]  
    public string Source { get; set; } = "inbound-call";

    public LeadStatus Status { get; set; } = LeadStatus.New;  // enum → string in config

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Location Location { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Guid? CustomerId { get; set; }
}
```

- `required` → EF infers `IsRequired()` automatically — no `.IsRequired()` in config needed
- `[MaxLength(N)]` → preferred over `[StringLength(N)]` for EF; maps directly to `varchar(N)`
- Nullable `string?` → EF infers `IsRequired(false)` automatically
- Default values in C# initializer (`= "inbound-call"`) are fine for app-level defaults; use `HasDefaultValue` in config only when the DB column default matters independently

### IEntityTypeConfiguration<T> — only what attributes can't do
```csharp
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        // PK default (DB-generated)
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");

        // Enum stored as string
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);

        // Timestamp column type
        builder.Property(o => o.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamp without time zone");

        // Indexes
        builder.HasIndex(o => o.TenantId);
        builder.HasIndex(o => new { o.TenantId, o.Status });
        builder.HasIndex(o => new { o.TenantId, o.CreatedAt });

        // Relationships + delete behavior
        builder.HasOne(o => o.Location)
               .WithMany(o => o.Leads)
               .HasForeignKey(o => o.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Customer)
               .WithMany(o => o.Leads)
               .HasForeignKey(o => o.CustomerId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## Npgsql / PostgreSQL Specifics
```csharp
// UUID default — always use gen_random_uuid() not newsequentialid()
builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");

// jsonb column — Dictionary<string, string> or complex object
builder.Property(o => o.StageIdMap)
       .HasColumnType("jsonb")
       .HasConversion(
           v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
           v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)!);

// Always store UTC; use timestamp without time zone (PostgreSQL default)
builder.Property(o => o.CreatedAt).HasColumnType("timestamp without time zone");

// Add Npgsql in Program.cs
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly("RadRoofer.Infrastructure")));
```

---

## Query Patterns
```csharp
// READ: always AsNoTracking — entities you won't write back
var leads = await db.Leads
    .AsNoTracking()
    .Where(o => o.Status == LeadStatus.New)
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync(ct);

// PAGINATE
var total = await query.CountAsync(ct);
var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

// WRITE: load with tracking (no AsNoTracking), mutate, SaveChangesAsync
var lead = await db.Leads.FirstOrDefaultAsync(o => o.Id == id, ct);
if (lead is null) return NotFound();
lead.Status = LeadStatus.Contacted;
await db.SaveChangesAsync(ct);

// BYPASS global filters (admin/seed only)
var allLeads = await db.Leads.IgnoreQueryFilters().ToListAsync(ct);
```

---

## Idempotency — Unique Index Pattern
```csharp
// Schema: unique index on CallLog.VapiCallId
builder.HasIndex(o => o.VapiCallId).IsUnique();

// Handler: catch unique constraint violation
try
{
    db.CallLogs.Add(callLog);
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                    && pg.SqlState == "23505") // unique_violation
{
    return Ok(); // already processed — idempotent
}
```

---

## Seed Data (HasData)
```csharp
// In OnModelCreating or entity configuration
// MUST use hardcoded Guids — EF compares on every migration
private static readonly Guid SeedTenantId = Guid.Parse("11111111-0000-0000-0000-000000000001");

modelBuilder.Entity<Tenant>().HasData(new Tenant
{
    Id = SeedTenantId,
    Name = "Smith Roofing",
    CreatedAt = new DateTime(2024, 1, 1, DateTimeKind.Utc),
    UpdatedAt = new DateTime(2024, 1, 1, DateTimeKind.Utc)
});
// Seed user: hash password with BCrypt.Net-Next before embedding
```

---

## Migrations
```bash
# Always specify project and startup project
dotnet ef migrations add InitialCreate \
  --project src/RadRoofer.Infrastructure \
  --startup-project src/RadRoofer.Api

dotnet ef database update \
  --project src/RadRoofer.Infrastructure \
  --startup-project src/RadRoofer.Api
```

---

## Common Mistakes
- Never `SaveChanges()` (sync) — always `SaveChangesAsync()`
- Never set `CreatedAt`/`UpdatedAt` in constructors or handlers — SaveChanges override handles it
- Never set `Id` — PostgreSQL generates it via `gen_random_uuid()`
- Never `.Include()` on write paths unless you need the nav property to write back
- Don't use lazy loading — always explicit `Include()` or split queries
- Don't call `IgnoreQueryFilters()` in normal application code — only seed/admin
- Don't store `DateTime.Now` — always `DateTime.UtcNow`
