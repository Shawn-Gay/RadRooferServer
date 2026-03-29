# Claude Code Setup — Roofers Tech

> Research-based plan for a high-signal Claude Code configuration tailored to this project.
> Date: 2026-03-23

---

## Overview: The Four Levers

| Layer | What It Does | When It Fires |
|---|---|---|
| **CLAUDE.md** | Persistent instructions Claude reads every session | Always (on load) |
| **Skills** | Reusable slash commands + auto-invoked context | On demand or when description matches |
| **Agents** | Specialized sub-AIs with scoped tools/model | Delegated or @-mentioned |
| **Hooks** | Deterministic shell scripts — 100% enforced | On specific lifecycle events |

The key insight: **CLAUDE.md tells Claude what to do; Hooks enforce it regardless.**

---

## 1. CLAUDE.md Plan

### Scope Strategy

```
~/.claude/CLAUDE.md                         ← global personal preferences (already exists)
D:/RadcoreAI/Roofers Tech/CLAUDE.md         ← project root — always loaded
D:/RadcoreAI/Roofers Tech/.claude/rules/    ← topic-specific rule files
```

### Root CLAUDE.md Structure

```markdown
# Roofers Tech

## Project
Multi-tenant SaaS AI receptionist for roofing companies. Built to sell commercially.
Tenant hierarchy: Platform → Roofer → Location → Customer/Job.

## Commands
- Build API:      dotnet build
- Run API:        dotnet run --project src/RadRoofer.Api
- Test:           dotnet test
- Format:         dotnet format
- Mobile dev:     npx expo start
- Mobile test:    npx jest

## Architecture Rules
- ALL data access must use EF Core global query filters (Finbuckle). Never bypass tenant isolation.
- CRM/Calendar integrations implement adapter interfaces — never hardcode a provider.
- Warm outbound only — no cold AI calls (legal). Qualifier: caller must have initiated first.

## Code Conventions
- Lambda params: prefer `o => o...`, fallback `x => x...` if second lambda in same scope.
- C# async methods: always suffix with `Async`.
- Entity IDs: Guid, never int.

## Testing
- Integration tests hit a real PostgreSQL database (never mock EF Core contexts).
- Test project: tests/RadRoofer.Tests
- Always run `dotnet test` after implementation changes.

## Git
- Branch format: feature/short-description, fix/short-description
- Commits: conventional commits style (feat:, fix:, chore:, refactor:)
- Never commit .env or secrets.

## Key Files
See @docs/ARCHITECTURE.md for system design.
See @docs/STACK.md for package decisions.
```

### Topic Rule Files (`.claude/rules/`)

**`security.md`** — scoped to `src/` paths:
```yaml
---
paths:
  - "src/**/*.cs"
---
# Security Rules
- Every controller action touching tenant data must include [Authorize] and rely on Finbuckle filters.
- Never expose internal IDs in API responses — use opaque slugs or GUIDs only.
- Input from Vapi webhooks is untrusted — always validate before processing.
```

**`database.md`** — scoped to migrations and DbContext:
```yaml
---
paths:
  - "src/**/*DbContext*"
  - "src/**/Migrations/**"
---
# Database Rules
- Always add `HasQueryFilter` in OnModelCreating for any new tenant-scoped entity.
- Migrations must be reversible (include Down() implementation).
- Index every foreign key and any column used in WHERE clauses.
```

---

## 2. Skills Plan

### Recommended Skills to Create

#### `/new-endpoint` — Scaffold a new ASP.NET Core endpoint
```yaml
---
name: new-endpoint
description: Scaffold a new API endpoint. Use when adding a new route or controller action.
argument-hint: "[HttpMethod] [route] [description]"
disable-model-invocation: false
---
Create a new ASP.NET Core endpoint: $ARGUMENTS

Follow existing controller patterns in src/RadRoofer.Api/Controllers/.
- Add [Authorize] attribute
- Use the standard ApiResponse<T> wrapper
- Add request/response DTOs in the appropriate Models folder
- Register any new services in Program.cs
- Write at least one integration test for the happy path
```

#### `/migration` — Create an EF Core migration
```yaml
---
name: migration
description: Generate a new EF Core migration after model changes. Use after adding or modifying entities.
argument-hint: "[migration-name]"
disable-model-invocation: true
---
Run: dotnet ef migrations add $ARGUMENTS --project src/RadRoofer.Infrastructure --startup-project src/RadRoofer.Api

Then review the generated migration file and confirm:
1. HasQueryFilter is present on any new tenant-scoped entity
2. Down() method is properly implemented
3. Indexes are added for FK columns
```

#### `/review` — Code review before commit
```yaml
---
name: review
description: Review recently changed code for quality, security, and tenant-isolation issues before committing.
context: fork
model: opus
---
Review all changes since the last commit: !`git diff HEAD`

Check for:
1. Tenant isolation — any EF Core query missing the global filter?
2. Security — SQL injection, unvalidated Vapi webhook input, exposed internal IDs
3. Async correctness — missing await, sync-over-async
4. Missing tests for new behavior
5. Conventional commit readiness

Output findings as: CRITICAL | WARNING | SUGGESTION with file:line references.
```

#### `/commit` — Smart conventional commit
```yaml
---
name: commit
description: Stage and commit changes with a well-formed conventional commit message.
disable-model-invocation: true
---
1. Run `git diff HEAD` and `git status` to understand what changed.
2. Stage relevant files (never .env, *.key, *.pem, appsettings.*.json with secrets).
3. Write a conventional commit message: feat|fix|refactor|chore|test: <short description>.
4. Commit with Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
5. Show the result.
```

#### `/vapi-handler` — Scaffold a Vapi webhook handler
```yaml
---
name: vapi-handler
description: Scaffold a new Vapi webhook event handler. Use when adding handling for a new Vapi event type.
argument-hint: "[event-type] [description]"
---
Scaffold a Vapi webhook handler for: $ARGUMENTS

Follow the existing handler pattern in src/RadRoofer.Api/Webhooks/Vapi/.
- Validate the Vapi signature header first
- Deserialize the event payload
- Route to the appropriate domain service
- Return 200 immediately (Vapi is fire-and-forget)
- Write a unit test for the handler logic
```

---

## 3. Agents Plan

### Recommended Custom Agents

#### `security-reviewer` — Tenant isolation + security audit
```yaml
---
name: security-reviewer
description: Reviews code for security vulnerabilities and tenant-isolation gaps. Use proactively after any auth, EF Core model, or webhook handling changes.
tools: Read, Grep, Glob, Bash
model: opus
---
You are a senior security engineer specializing in multi-tenant SaaS on ASP.NET Core.

Focus on:
- Finbuckle tenant isolation — missing HasQueryFilter, bypassed filters
- Authorization gaps — missing [Authorize], missing role checks
- Vapi webhook input not validated before processing
- Secrets or connection strings in code
- GUID vs int ID leakage in API responses

Always:
1. Run `git diff HEAD` to see what changed
2. Read affected files in full
3. Output: CRITICAL | WARNING | SUGGESTION with file:line and fix suggestion
```

#### `db-architect` — EF Core / PostgreSQL specialist
```yaml
---
name: db-architect
description: Designs EF Core models, migrations, and PostgreSQL schema. Use when adding entities, planning schema changes, or reviewing migrations.
tools: Read, Grep, Glob, Bash
model: sonnet
---
You are a database architect specializing in EF Core with PostgreSQL in multi-tenant SaaS.

Rules for this project:
- All tenant-scoped entities must have a TenantId and a HasQueryFilter in OnModelCreating
- Guid PKs only
- Always index FKs and frequently-filtered columns
- Migrations must include proper Down() implementations
- Use owned entities for value objects, not separate tables

When reviewing migrations: check for missing indexes, missing query filters, data-loss risk in Down().
```

#### `api-designer` — REST API design
```yaml
---
name: api-designer
description: Designs REST API contracts, DTOs, and response shapes. Use when planning new API surface or reviewing endpoint design.
tools: Read, Grep, Glob
model: sonnet
---
You are a REST API designer for an ASP.NET Core SaaS product.

Standards for this project:
- Resources follow tenant hierarchy: /api/v1/locations/{locationId}/jobs/{jobId}
- Use ApiResponse<T> wrapper for all responses
- DTOs live in separate request/response classes, never expose EF entities directly
- Validation via FluentValidation, not DataAnnotations
- 400 for validation errors, 403 for tenant boundary violations, 404 for not found

When designing: propose the full route, request DTO, response DTO, and any validation rules.
```

---

## 4. Hooks Plan

### Recommended Hooks

#### `PostToolUse` — Auto-format after C# file edits
```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "dotnet format --include $CLAUDE_TOOL_OUTPUT_FILE_PATH 2>/dev/null || true",
            "async": true
          }
        ]
      }
    ]
  }
}
```

#### `PreToolUse` — Block writes to secrets files
```bash
#!/bin/bash
# .claude/hooks/block-secrets.sh
FILE=$(echo "$CLAUDE_TOOL_INPUT" | jq -r '.file_path // empty')
if [[ "$FILE" =~ \.(env|key|pem|p12|pfx)$ ]] || [[ "$FILE" =~ appsettings\.(Production|Staging) ]]; then
  echo "Blocked: Writing to secrets/production config is not allowed." >&2
  exit 2
fi
exit 0
```

#### `Stop` — Remind to run tests before finishing
```bash
#!/bin/bash
# .claude/hooks/remind-tests.sh
# Inject a reminder into Claude's context if .cs files were modified
if git diff --name-only HEAD 2>/dev/null | grep -q '\.cs$'; then
  echo '{"hookSpecificOutput": {"additionalContext": "C# files were modified. Remind the user to run dotnet test if they have not already."}}'
fi
exit 0
```

#### `SessionStart` — Re-inject Critical Context After Auto-Compaction
When context auto-compacts (~95% full), early instructions can be lost. This hook fires after compaction and re-injects a reminder:

```json
{
  "hooks": {
    "SessionStart": [
      {
        "matcher": "compact",
        "hooks": [
          {
            "type": "command",
            "command": "echo 'Context was compacted. Key rules: (1) All EF queries must have TenantId via Finbuckle global filters — never bypass. (2) GUIDs only for PKs. (3) Soft deletes on Lead/Customer/Job. (4) Run dotnet test after any .cs change.'"
          }
        ]
      }
    ]
  }
}
```

#### Settings.json Hook Config (`.claude/settings.json`)
```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [
          {
            "type": "command",
            "command": "bash .claude/hooks/block-secrets.sh",
            "timeout": 5
          }
        ]
      }
    ],
    "Stop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "bash .claude/hooks/remind-tests.sh",
            "timeout": 5
          }
        ]
      }
    ]
  }
}
```

---

## 5. Settings Plan

### `.claude/settings.json` (committed, team-wide)
```json
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "allow": [
      "Bash(dotnet build *)",
      "Bash(dotnet test *)",
      "Bash(dotnet run *)",
      "Bash(dotnet format *)",
      "Bash(dotnet ef *)",
      "Bash(npx expo *)",
      "Bash(npx jest *)",
      "Bash(git diff *)",
      "Bash(git status)",
      "Bash(git log *)",
      "Bash(git add *)",
      "Bash(git commit *)"
    ],
    "deny": [
      "Read(./.env)",
      "Read(./.env.*)",
      "Read(./src/**/appsettings.Production.json)",
      "Read(./src/**/appsettings.Staging.json)"
    ],
    "ask": [
      "Bash(git push *)",
      "Bash(git reset *)",
      "Bash(dotnet ef database *)"
    ]
  },
  "includeGitInstructions": true,
  "model": "claude-sonnet-4-6"
}
```

### `.claude/settings.local.json` (gitignored, personal overrides)
```json
{
  "permissions": {
    "defaultMode": "acceptEdits"
  }
}
```

---

## 6. MCP Servers to Consider

| Server | Value for This Project |
|---|---|
| **PostgreSQL MCP** | Let Claude query the schema directly — speeds up migration and model work |
| **GitHub MCP** | Read issues/PRs from within the session without switching context |
| **Playwright MCP** | End-to-end testing of the mobile app or API if we add a web frontend |
| **Memory MCP** | Persistent cross-session key-value store (supplement auto-memory for structured data) |

### PostgreSQL Setup (high value, do first)
```bash
claude mcp add postgres -- npx -y @modelcontextprotocol/server-postgres "postgresql://localhost/roofers_tech_dev"
```

---

## 7. File/Folder Structure to Create

```
D:/RadcoreAI/Roofers Tech/
├── CLAUDE.md                          ← project instructions (create)
├── .claude/
│   ├── settings.json                  ← team permissions (create)
│   ├── settings.local.json            ← personal overrides (create, gitignore)
│   ├── rules/
│   │   ├── security.md                ← tenant isolation rules (create)
│   │   └── database.md                ← EF Core/migration rules (create)
│   ├── skills/
│   │   ├── new-endpoint/SKILL.md      ← scaffold endpoint (create)
│   │   ├── migration/SKILL.md         ← EF migration helper (create)
│   │   ├── review/SKILL.md            ← pre-commit code review (create)
│   │   ├── commit/SKILL.md            ← conventional commit (create)
│   │   └── vapi-handler/SKILL.md      ← Vapi webhook scaffolding (create)
│   ├── agents/
│   │   ├── security-reviewer.md       ← tenant/security audit agent (create)
│   │   ├── db-architect.md            ← EF Core/PostgreSQL agent (create)
│   │   └── api-designer.md            ← REST API design agent (create)
│   └── hooks/
│       ├── block-secrets.sh           ← deny writes to secrets (create)
│       └── remind-tests.sh            ← post-stop test reminder (create)
```

---

## 8. Priority Order (What to Set Up First)

1. **CLAUDE.md** — highest leverage, immediately improves every session
2. **`.claude/settings.json`** — pre-approves safe dotnet/git commands, stops prompts
3. **`/review` skill** — biggest quality/safety win before any commit
4. **`/commit` skill** — consistent conventional commits, never forget co-author
5. **`security-reviewer` agent** — catches tenant isolation issues proactively
6. **Hooks** — block-secrets.sh first, then test reminder
7. **`/new-endpoint` + `/migration` skills** — speed up the most repetitive scaffolding
8. **PostgreSQL MCP** — schema-aware assistance for EF Core work
9. **Remaining agents** (`db-architect`, `api-designer`) — polish

---

## Key Principles Learned

- **CLAUDE.md is context, not enforcement** — hooks are enforcement. Use both.
- **Keep CLAUDE.md under 200 lines** — longer reduces adherence.
- **Write agent `description` fields carefully** — Claude auto-delegates based on them.
- **Subagents protect your context window** — send file-heavy exploration to them.
- **`disable-model-invocation: true` on skills with side effects** — commits, deploys, migrations.
- **Integration tests must hit real PostgreSQL** — never mock EF Core (per prior project decision).
- **`/clear` between unrelated tasks** — stale context degrades performance.

---

## 9. Coding Efficiency Playbook

Rules an LLM should follow during every coding session. Condensed from research — enough to internalize, not a tutorial.

### CLAUDE.md Adherence

- **<200 lines per file** — compliance drops measurably above this. Prune anything Claude already does by default.
- **Concrete > vague** — "Use 2-space indentation" beats "format code properly". Claude follows verifiable rules better.
- **Move reference material out** — use `@path/to/file` imports for docs, schemas, changelogs. They load on demand, not every session.
- **Scoped rules are more effective** — `.claude/rules/security.md` with `paths: ["src/**/*.cs"]` loads only when working in C#. Keeps other sessions lean.
- **No contradictions** — if two rules conflict, Claude picks one arbitrarily. Check parent/child CLAUDE.md files for conflicts.

### Context Window Thresholds

- **>60% capacity = quality degrades** — earlier instructions get less attention weight. Don't wait for the hard limit.
- **Auto-compaction fires at ~95%** — what survives: code snippets, file lists. What gets lost: early instructions, exploration notes.
- **After compaction** — a `SessionStart` hook with matcher `"compact"` can re-inject critical context that would otherwise be lost.

### Plan Mode: When to Use vs Skip

| Use Plan Mode | Skip It |
|---|---|
| Multi-file refactor with unclear approach | Single-file fix with obvious solution |
| Architecture decision with tradeoffs | Adding a new endpoint following existing pattern |
| Unfamiliar codebase, need to explore first | Variable rename, typo fix, log line |
| Changes where direction matters more than speed | Well-defined task with clear acceptance criteria |

**Insight**: Plan mode overhead is real. Only use it when the planning phase prevents multiple correction rounds.

### Parallel Tool Calls

Run independent tools in one message, not sequentially:
- Two Grep searches on unrelated patterns → parallel
- Glob to find files + Read on already-known files → parallel
- Multiple Bash commands with no output dependency → parallel

**Only sequence when Tool B needs Tool A's output.**

### Give Claude Success Criteria

Instead of: *"Implement email validation"*

Use: *"Write validateEmail. Test cases: 'user@example.com' → true, 'invalid' → false. Run tests after."*

With concrete pass/fail criteria, Claude verifies its own work and iterates. Without them, it ships and stops.

### Multi-File Refactor Pattern

1. Build task list in dependency order (foundational files first, consumers second, tests last)
2. Implement + verify per group — don't change everything then test everything
3. Use a subagent for investigation ("which files import X?") — keeps verbose results out of main context
4. Commit once at the end after all tests pass

### Model Selection Per Agent

| Model | Use For |
|---|---|
| `haiku` | Fast research, Explore agents, read-only codebase scanning |
| `sonnet` | Balanced code analysis, reviews, most implementation work |
| `opus` | Complex architectural decisions, security audits, hard bugs |

Don't default everything to Opus — Haiku exploration + Sonnet implementation is faster and cheaper for most work.

### Verification After Every Logical Group

Run affected tests after each group of related changes, not just at the end. Bugs caught early are cheaper to fix. For this project: `dotnet test --filter <category>` after each controller/entity group.

---

## 10. Context Window Efficiency

The `/context` command shows token usage by category. The most common drain is file reads.

### File Read Hygiene

**Use `offset` + `limit` for large files** — never read a 500-line file when you only need lines 100–150:
```
Read file: src/RadRoofer.Infrastructure/Data/AppDbContext.cs, offset: 100, limit: 50
```

**Don't re-read files already in context** — if a file was read earlier in the session, reference that earlier read instead of issuing a new Read call. The content is already in the message history.

**Grep before Read** — use Grep to find the exact line/block you need, then Read only that section with offset/limit. Avoids reading entire files to find one method.

**Delegate heavy exploration to subagents** — when a task requires reading 10+ files (e.g. "understand the full CRM adapter pattern"), use an Explore subagent. The subagent's file reads don't count against the main context window.

### When to `/clear`

`/clear` resets the context window entirely. Use it:
- Between unrelated tasks (e.g. after finishing schema work, before starting API work)
- When context usage exceeds ~70% and the current task is wrapping up
- After a long planning session before starting implementation

Note: `/clear` discards all prior file reads from context. If you'll need the same files again in the next task, they'll need to be re-read — that's fine and usually worth it for the context savings.

### Rule of Thumb

| Situation | Approach |
|---|---|
| Need one method from a large file | Grep for it first, then Read with offset/limit |
| Need to understand a whole module | Delegate to Explore subagent |
| Already read a file this session | Reference the earlier read, don't re-read |
| Task is done, next task is unrelated | `/clear` before starting |
| Context > 70% mid-task | Finish current task, then `/clear` |
