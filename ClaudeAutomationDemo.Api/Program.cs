using ClaudeAutomationDemo.Api.Data;
using ClaudeAutomationDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=claudeautomationdemo.db"));

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

orders.MapPost("/", async (Order order, AppDbContext db) =>
{
    if (order.Quantity <= 0)
    {
        return Results.Problem("Quantity must be greater than zero.", statusCode: 400);
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

// Exposed so the integration test project can drive the real app via
// WebApplicationFactory<Program>.
public partial class Program;
