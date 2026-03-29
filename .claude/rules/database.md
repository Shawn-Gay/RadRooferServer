---
description: EF Core and migration rules, scoped to DbContext and migration files
paths:
  - "src/**/*DbContext*"
  - "src/**/*Configuration*"
  - "src/**/Migrations/**"
---

# Database Rules

## New Entities
- Add a `DbSet<T>` to `AppDbContext`
- Add `HasQueryFilter` in `OnModelCreating` — every tenant-scoped entity gets one
- Create a corresponding `IEntityTypeConfiguration<T>` class in `Infrastructure/Data/Configurations/`
- Add a `HasDefaultValueSql("gen_random_uuid()")` for the `Id` property

## Migrations
- Always specify both `--project` and `--startup-project` flags
- Review generated migration before applying — check for:
  - Missing `HasQueryFilter` on new entities
  - Missing indexes on FK columns and frequently-filtered columns
  - `Down()` implementation is complete and reversible
  - No accidental data-loss columns (dropped columns with data)

## Indexes
- Index every FK column: `builder.HasIndex(o => o.TenantId)`
- Index compound filters used in queries: `builder.HasIndex(o => new { o.TenantId, o.Status })`
- Index `VapiCallId` as unique: `builder.HasIndex(o => o.VapiCallId).IsUnique()`

## Timestamps
- `CreatedAt` and `UpdatedAt` are set in `SaveChangesAsync` override — never set manually
- All timestamp properties use `DateTimeOffset`, never `DateTime`
- All timestamp columns: `HasColumnType("timestamp with time zone")`
- Seed data timestamps: `new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero)`

## Enums
- Always store as string: `builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30)`
- Never store as int — string values survive enum reorders

## Shadow FK Properties
- **Never** use `EF.Property<Guid>(o, "ServiceLocationId")` in LINQ projections or WHERE clauses — use the navigation property instead: `o.ServiceLocation.Id`
- `EF.Property<>` is only valid inside `HasQueryFilter` expressions in `OnModelCreating`, where there is no navigation property available
- In controllers: `o.ServiceLocation.Id`, `o.Organization.Id`, etc. — EF translates these to the FK column without a JOIN when used in a projection

## Soft Deletes
- `Lead`, `Customer`, `Job` use soft delete — never issue DELETE; `SaveChangesAsync` converts to IsDeleted=true
- Soft-delete global filter is already in `AppDbContext` — no additional filter needed in queries
