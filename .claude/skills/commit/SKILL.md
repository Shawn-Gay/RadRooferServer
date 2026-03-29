---
name: commit
description: Stage and commit changes with a well-formed conventional commit message. Use when ready to commit completed work.
disable-model-invocation: false
---

1. Run `git diff HEAD` and `git status` to understand what changed.

2. Stage relevant files — never stage:
   - `.env` or `.env.*`
   - `*.key`, `*.pem`, `*.p12`, `*.pfx`
   - `appsettings.Production.json` or `appsettings.Staging.json`
   - Any file with a hardcoded secret or connection string

3. Write a conventional commit message:
   - Format: `type(scope): short description`
   - Types: `feat` | `fix` | `refactor` | `chore` | `test` | `docs`
   - Scope (optional): area of the codebase — e.g. `feat(webhooks):`, `fix(auth):`
   - Description: imperative mood, lowercase, no period — "add CallLog entity" not "Added CallLog entity"
   - Keep the subject line under 72 characters

4. Commit with:
   ```
   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   ```

5. Show the commit hash and message after success.
