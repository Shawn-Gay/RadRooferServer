---
name: new-endpoint
description: Scaffold a new ASP.NET Core API endpoint following project conventions. Use when adding a new controller action or route.
argument-hint: "[HttpMethod] [route] [brief description]"
---

Read `.claude/tech/aspnetcore9.md` and `.claude/tech/csharp-dotnet9.md` before writing any code.

Scaffold a new ASP.NET Core endpoint: $ARGUMENTS

Follow these conventions:

**Controller**
- Add `[Authorize]` on the controller or action
- Return `ActionResult<T>` (not `IActionResult`) for better OpenAPI inference
- Thin controller: validate input → query/write DB → return DTO. No service class unless the plan says so.
- Primary constructor for DI: `public class FooController(AppDbContext db) : ControllerBase`

**DTOs**
- Request: `record` with `required` + DataAnnotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`)
- Response: `record` with `required init` properties
- Place in `RadRoofer.Core/DTOs/` — one file per DTO

**Database**
- Read queries: always `.AsNoTracking()`
- Write queries: load with tracking → mutate → `SaveChangesAsync(ct)`
- Global query filters handle tenant scoping — never add `.Where(o => o.TenantId == ...)` manually
- Paginate with `Skip((page - 1) * pageSize).Take(pageSize)` + a separate `CountAsync`

**After writing the code**
- Confirm `[Authorize]` is present
- Confirm no manual TenantId filtering
- Confirm `.AsNoTracking()` on reads
- Confirm the DTO never exposes the EF entity directly
