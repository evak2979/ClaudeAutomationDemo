# Claude Automation Demo

A minimal ASP.NET Core Web API + EF Core (SQLite) project, scaffolded as the
companion walkthrough for the [Claude Automation Manual](../claude-automation-manual-site).

## First run

This repo was written by hand (no `dotnet new`, no network access at scaffold
time), so the very first thing to do is let `dotnet` verify it:

```bash
dotnet restore
dotnet build
dotnet ef database update                # applies InitialCreate + AddOrderProcessDate
dotnet test ClaudeAutomationDemo.sln
dotnet run --project ClaudeAutomationDemo.Api
```

If `dotnet ef` isn't found: `dotnet tool install --global dotnet-ef`.

## What's here

- `ClaudeAutomationDemo.Api/` — the Web API: `/orders` endpoints (GET all, GET by id,
  POST with validation), EF Core `DbContext`, and the `Order` model
- `ClaudeAutomationDemo.Api.UnitTests/` — model and JSON-contract tests
- `ClaudeAutomationDemo.Api.IntegrationTests/` — end-to-end `/orders` tests plus
  migration upgrade/rollback tests against a real SQLite database
- `CLAUDE.md` — project memory for Claude Code (build commands, conventions, what's
  intentionally left unfinished)
- `.claude/settings.json` — allow/deny permission rules scoped to this repo

## Migrations

`InitialCreate` was generated as the first walkthrough step; `AddOrderProcessDate`
follows it and adds the nullable `Order.ProcessDate` column. Both are regenerated
with `dotnet ef migrations add` — never hand-edited.
