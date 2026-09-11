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
- Validation rules live in `ClaudeAutomationDemo.Api/Validation/OrderValidator.cs` as a pure
  function returning the first failure message (or null); the endpoint turns that into
  `Results.Problem(...)` with a 400 — see `POST /orders` as the pattern to follow
- Time-dependent rules take the current instant as a parameter and the endpoint passes
  `TimeProvider.GetUtcNow()`, so they can be tested without waiting for the clock
- Classes for EF-tracked entities, records for anything that's a pure DTO

## Status / next steps
- Migrations now exist: `InitialCreate`, then `AddOrderProcessDate` (adds the nullable
  `Order.ProcessDate` column). Apply them with `dotnet ef database update`.
- Don't hand-edit anything under `Migrations/` — regenerate with
  `dotnet ef migrations add` instead.
- `Order.ProcessDate` is nullable on purpose: orders created before the field existed,
  and orders not processed yet, both read back as null. Keep it optional on input.
- A submitted `ProcessDate` may be at most `OrderValidator.MaxProcessDateLeadDays` (7) days
  ahead of now. Past dates are allowed, and the rule guards new input only — rows already
  stored outside the window stay readable.
