---
name: migration
description: Generate a new EF Core migration after entity or configuration changes. Use after adding or modifying entities.
argument-hint: "[MigrationName]"
disable-model-invocation: false
---

Read `.claude/tech/efcore9.md` before reviewing the migration output.

Run the migration command:
```
dotnet ef migrations add $ARGUMENTS --project src/RadRoofer.Infrastructure --startup-project src/RadRoofer.Api
```

After the migration file is generated, read it and verify:

1. **Query filters** — any new tenant-scoped entity has `HasQueryFilter` in `OnModelCreating`
2. **Indexes** — FK columns are indexed; any column used in `WHERE` clauses is indexed
3. **Enums** — stored as `nvarchar` (string), not `int`
4. **Timestamps** — column type is `timestamp without time zone`
5. **Down() method** — fully implemented and reversible (no empty `Down()`)
6. **No data loss** — no `DropColumn` on a column that might have data without a plan

Report any issues found before the migration is applied.
