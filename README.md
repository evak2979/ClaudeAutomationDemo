# Claude Automation Demo

A minimal ASP.NET Core Web API + EF Core (SQLite) project, scaffolded as the
companion walkthrough for the [Claude Automation Manual](../claude-automation-manual-site).

## First run

This repo was written by hand (no `dotnet new`, no network access at scaffold
time), so the very first thing to do is let `dotnet` verify it:

```bash
dotnet restore
dotnet ef migrations add InitialCreate   # no migration exists yet — this is step one
dotnet build
dotnet run --project ClaudeAutomationDemo.Api
```

If `dotnet ef` isn't found: `dotnet tool install --global dotnet-ef`.

## What's here

- `ClaudeAutomationDemo.Api/` — the Web API: `/orders` endpoints (GET all, GET by id,
  POST with validation), EF Core `DbContext`, and the `Order` model
- `CLAUDE.md` — project memory for Claude Code (build commands, conventions, what's
  intentionally left unfinished)
- `.claude/settings.json` — allow/deny permission rules scoped to this repo

## Why no migration yet

Deliberately left out — generating `InitialCreate` is meant to be the first thing
you watch Claude Code actually do in this repo, matching the CLAUDE.md walkthrough.
