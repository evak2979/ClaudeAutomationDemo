# CLAUDE.md

## Build & run
- `dotnet build ClaudeAutomationDemo.sln`
- `dotnet run --project ClaudeAutomationDemo.Api`
- `dotnet test ClaudeAutomationDemo.sln`
- `dotnet format` before committing

## Architecture
- Minimal API in `Program.cs`, endpoints grouped by resource (`/orders`, more to come)
- EF Core + SQLite for local dev — connection string lives in `appsettings.json`
- Models in `ClaudeAutomationDemo.Api/Models/`, `DbContext` in `ClaudeAutomationDemo.Api/Data/`
- Tests split by kind: `ClaudeAutomationDemo.Api.UnitTests` (pure model/contract assertions)
  and `ClaudeAutomationDemo.Api.IntegrationTests` (real HTTP pipeline via
  `WebApplicationFactory<Program>` against an in-memory SQLite DB built by the real migrations)

## Conventions
- Validate in the endpoint, return `Results.Problem(...)` with a 400 for bad input — see the
  `Quantity <= 0` check in the `POST /orders` endpoint as the pattern to follow
- Classes for EF-tracked entities, records for anything that's a pure DTO

## Status / next steps
- Migrations now exist: `InitialCreate`, then `AddOrderProcessDate` (adds the nullable
  `Order.ProcessDate` column). Apply them with `dotnet ef database update`.
- Don't hand-edit anything under `Migrations/` — regenerate with
  `dotnet ef migrations add` instead.
- `Order.ProcessDate` is nullable on purpose: orders created before the field existed,
  and orders not processed yet, both read back as null. Keep it optional on input.
