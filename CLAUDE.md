# CLAUDE.md

## Build & run
- `dotnet build ClaudeAutomationDemo.sln`
- `dotnet run --project ClaudeAutomationDemo.Api`
- `dotnet format` before committing

## Architecture
- Minimal API in `Program.cs`, endpoints grouped by resource (`/orders`, more to come)
- EF Core + SQLite for local dev — connection string lives in `appsettings.json`
- Models in `ClaudeAutomationDemo.Api/Models/`, `DbContext` in `ClaudeAutomationDemo.Api/Data/`

## Conventions
- Validate in the endpoint, return `Results.Problem(...)` with a 400 for bad input — see the
  `Quantity <= 0` check in the `POST /orders` endpoint as the pattern to follow
- Classes for EF-tracked entities, records for anything that's a pure DTO

## Status / next steps
- No EF Core migration exists yet — this repo was scaffolded without one on purpose.
  The first task is `dotnet ef migrations add InitialCreate`, then
  `dotnet ef database update` (or just let the app create it — see Program.cs).
- Once migrations exist: don't hand-edit anything under `Migrations/` — regenerate
  with `dotnet ef migrations add` instead.
