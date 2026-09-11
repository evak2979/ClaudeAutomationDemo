using ClaudeAutomationDemo.Api.Data;
using ClaudeAutomationDemo.Api.Models;
using ClaudeAutomationDemo.Api.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=claudeautomationdemo.db"));

// Injectable clock so the ProcessDate window can be exercised deterministically.
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var orders = app.MapGroup("/orders");

orders.MapGet("/", async (AppDbContext db) =>
    await db.Orders.ToListAsync());

orders.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Orders.FindAsync(id) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

orders.MapPost("/", async (Order order, AppDbContext db, TimeProvider timeProvider) =>
{
    if (OrderValidator.Validate(order, timeProvider.GetUtcNow()) is { } error)
    {
        return Results.Problem(error, statusCode: 400);
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

// Exposed so the integration test project can drive the real app via
// WebApplicationFactory<Program>.
public partial class Program;
