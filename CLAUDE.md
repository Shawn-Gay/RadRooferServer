# Roofers Tech — Project Rules

## Context
Multi-tenant SaaS AI receptionist for roofing companies. Stack: .NET 9 / C# 13, ASP.NET Core 9,
EF Core 9 + Npgsql, PostgreSQL, Finbuckle.MultiTenant, Railway hosting.
Hierarchy: Platform → Tenant (roofer) → Location → Customers/Jobs.

## Tech References
Read the relevant doc **before** writing code for that technology. Use the Read tool — do NOT rely on training knowledge for version-specific patterns.

| Task | Read first |
|---|---|
| Any C# / .NET code | `.claude/tech/csharp-dotnet9.md` |
| DbContext, migrations, EF queries, entity configs | `.claude/tech/efcore9.md` |
| Controllers, auth, filters, middleware, Program.cs | `.claude/tech/aspnetcore9.md` |

---

## Code Style
- Lambda params: `o => o...` first choice; `x => x...` if a second lambda is needed in same scope
- File-scoped namespaces everywhere: `namespace RadRoofer.Core.Entities;`
- DTOs are `record` types with `required init` properties
- Domain entities are `class` types (EF requires mutable)
- Enums live in `RadRoofer.Core/Enums/`, one file per enum

### Enum Formatting
Always use multi-line format — never single-line:
```csharp
// correct
public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
}

// wrong — never do this
public enum LeadStatus { New, Contacted, Qualified }
```

### Attribute Formatting
Each attribute goes on its own line — never stack multiple on one line:
```csharp
// correct
[Required]
[EmailAddress]
string Email,

// wrong — never do this
[Required][EmailAddress] string Email,
```

---

## Architecture Rules

### Multi-Tenancy (non-negotiable)
- Every entity has `TenantId` — no exceptions
- **Never** manually filter `WHERE TenantId = ...` — EF global query filters do this automatically
- `IgnoreQueryFilters()` only in seed/migration/admin operations
- `AppDbContext` receives `ITenantInfo` via DI — never pass TenantId as a method parameter

### Soft Deletes
- `Lead`, `Customer`, `Job` use soft deletes — set `IsDeleted = true`, **never** DELETE rows
- The `SaveChangesAsync` override intercepts `EntityState.Deleted` and converts to soft delete
- `IsDeleted` is part of the global query filter — soft-deleted rows are invisible by default

### Timestamps
- `CreatedAt` / `UpdatedAt` are set in the `SaveChangesAsync` override — **never** set them manually in handlers or constructors

### PKs
- All entities use `Guid` PKs with `HasDefaultValueSql("gen_random_uuid()")`
- Never set `Id` manually in application code — let PostgreSQL generate it

### Data Access
- All DB writes go through `AppDbContext` — no raw SQL inserts
- Read queries: always add `.AsNoTracking()` — never track entities you aren't writing back
- Controllers are thin: validate input → query/write DB → return DTO. No service classes for simple operations.

### Auth
- JWT Bearer for all `/v1/*` routes
- Cookie auth for Razor Pages (`/Pages/*`)
- `PolicyScheme` routes by path prefix — both share the same claims including `tenant_id`
- `VapiSecretAuthFilter` verifies `X-Vapi-Secret` header **before** Finbuckle resolves tenant
- Passwords hashed with BCrypt.Net-Next — never store plaintext

---

## What NOT to Do
- Do not add `ILogger` injection unless logging is explicitly part of the task
- Do not wrap methods in `try/catch` unless error handling is explicitly requested
- Do not add XML doc comments (`///`)
- Do not use `var` when the type isn't obvious from the right-hand side
- Do not use `async void` — always `async Task`
- Do not use `.Result` or `.Wait()` on Tasks — always `await`
- Do not create interfaces or base classes unless the plan explicitly calls for them
- Do not create a service class just to wrap a single DB query
- Do not add constructor parameters for things the plan doesn't require
