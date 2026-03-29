# C# 13 / .NET 9 — Patterns Reference (LLM)

## File Conventions
```csharp
// File-scoped namespace — always, no braces
namespace RadRoofer.Core.Entities;

// One type per file, file name = type name
// Global usings in GlobalUsings.cs per project
global using System.Text.Json;
global using Microsoft.EntityFrameworkCore;
```

---

## Type Design

### Records — use for DTOs (immutable, value equality, no EF tracking)
```csharp
// Positional — concise for simple input/output
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);

// Property syntax — when DataAnnotations or doc comments are needed
public record CallLogDto
{
    public required Guid Id { get; init; }
    public required string CallerPhone { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

### Classes — use for domain entities (EF requires mutable)
```csharp
public class Lead
{
    public Guid Id { get; set; }
    public required Guid TenantId { get; set; }
    public required Guid LocationId { get; set; }
    public required string CallerName { get; set; }
    // navigation properties
    public Tenant Tenant { get; set; } = null!;
}
```

---

## Nullable Reference Types
- Enable in every `.csproj`: `<Nullable>enable</Nullable>`
- Reference types are non-null by default; `?` = explicitly nullable
- `required` keyword = must be set at object initialisation — use on non-nullable entity/record properties
- Navigation properties that EF guarantees will be loaded: suffix with `= null!`

---

## Primary Constructors (C# 12+) — use for DI in services/controllers
```csharp
// Parameters are in scope for the entire class body
public class VapiWebhookHandler(AppDbContext db, IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
{
    public async Task HandleAsync(VapiPayload payload, CancellationToken ct)
    {
        // db and tenantAccessor available directly — no field assignment needed
    }
}

// Do NOT use primary constructors on EF entities — parameterless ctor required
```

---

## Pattern Matching — prefer over if/else chains
```csharp
// Switch expression (exhaustive, returns value)
var label = status switch
{
    LeadStatus.New        => "New",
    LeadStatus.Contacted  => "Contacted",
    LeadStatus.Converted  => "Converted",
    _                     => "Unknown"
};

// Type pattern with deconstruct
if (result is ErrorResult { Message: var msg, Code: >= 500 }) { ... }

// Null check
if (customer is null) return NotFound();
```

---

## Collections
```csharp
// Collection expression (C# 12+)
List<string> tags = ["inbound-call", "web"];
string[] merged = [.. existingTags, "new-tag"];

// LINQ for transforms — prefer over foreach
var dtos = leads.Select(o => new LeadDto { Id = o.Id, ... }).ToList();

// Spread in method calls (C# 13 params collections)
LogWarning([.. contextData, "extra"]);
```

---

## Async / Await
```csharp
// Always suffix with Async, always return Task or Task<T>
public async Task<Lead> CreateLeadAsync(CreateLeadRequest request, CancellationToken ct)

// ConfigureAwait(false) NOT needed in ASP.NET Core — no sync context
// Never .Result / .Wait() — deadlock risk and hides exceptions
// Never async void — exceptions are uncatchable
```

---

## String Handling
```csharp
// Interpolation over string.Format
var msg = $"Hi {name}, your call was received.";

// Raw string literals for multiline JSON/SQL
var json = """
    {
        "event": "end-of-call-report"
    }
    """;

// CallerArgumentExpression for guard clauses
ArgumentNullException.ThrowIfNull(locationId);
ArgumentException.ThrowIfNullOrWhiteSpace(secret);
```

---

## Enums
```csharp
// In RadRoofer.Core/Enums/ — one file per enum
public enum LeadStatus { New, Contacted, Qualified, Converted, Lost }

// Convert to/from string in EF via value converter (see efcore9.md)
// Never store magic strings — always use enum members
```

---

## Common Mistakes
- Don't use `dynamic` — use generics or concrete types
- Don't use `Tuple<>` — use named records or `(Type name, Type name)` value tuples
- Don't nest ternary operators
- Don't shadow outer `o` lambda param — use `x` for inner lambda
- Don't `catch (Exception)` unless re-throwing or at an outermost boundary
- Don't use `Thread.Sleep` — use `await Task.Delay`
- Don't use `object` as a return type — use generics or discriminated union records
