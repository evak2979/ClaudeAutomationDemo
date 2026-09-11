using ClaudeAutomationDemo.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace ClaudeAutomationDemo.Api.IntegrationTests;

/// <summary>
/// Proves the upgrade path for a database that predates <c>ProcessDate</c>:
/// migrate to <c>InitialCreate</c> only, write rows against that older schema,
/// then apply <c>AddOrderProcessDate</c> and read everything back.
/// </summary>
public class ProcessDateMigrationTests
{
    private static AppDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    [Fact]
    public async Task ApplyingMigration_ToOldDatabase_LeavesExistingOrdersIntactWithNullProcessDate()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var createdAt = new DateTimeOffset(2025, 9, 1, 8, 0, 0, TimeSpan.Zero);

        // --- The world before this change: schema at InitialCreate, with data in it.
        await using (var db = CreateContext(connection))
        {
            await db.GetService<IMigrator>().MigrateAsync("InitialCreate");

            Assert.False(await ColumnExists(connection, "ProcessDate"));

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Orders (Id, CustomerName, Quantity, UnitPrice, CreatedAt)
                VALUES ({0}, {1}, {2}, {3}, {4})
                """,
                1, "Existing Customer", 5, 20.00m, createdAt);
        }

        // --- Apply the new migration on top of it.
        await using (var db = CreateContext(connection))
        {
            await db.Database.MigrateAsync();

            Assert.True(await ColumnExists(connection, "ProcessDate"));

            var existing = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == 1);
            Assert.Null(existing.ProcessDate);
            Assert.Equal("Existing Customer", existing.CustomerName);
            Assert.Equal(5, existing.Quantity);
            Assert.Equal(20.00m, existing.UnitPrice);
            Assert.Equal(createdAt, existing.CreatedAt);

            // And new rows can use the field straight away.
            var processedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            db.Orders.Add(new Api.Models.Order
            {
                CustomerName = "New Customer",
                Quantity = 1,
                UnitPrice = 1.00m,
                CreatedAt = processedAt,
                ProcessDate = processedAt
            });
            await db.SaveChangesAsync();

            Assert.Equal(
                processedAt,
                await db.Orders.AsNoTracking()
                    .Where(o => o.CustomerName == "New Customer")
                    .Select(o => o.ProcessDate)
                    .SingleAsync());
        }
    }

    [Fact]
    public async Task MigrationIsReversible_DroppingProcessDateAndKeepingOrders()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var db = CreateContext(connection);
        await db.Database.MigrateAsync();

        db.Orders.Add(new Api.Models.Order
        {
            CustomerName = "Rollback Customer",
            Quantity = 3,
            UnitPrice = 7.50m,
            ProcessDate = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        await db.GetService<IMigrator>().MigrateAsync("InitialCreate");

        Assert.False(await ColumnExists(connection, "ProcessDate"));

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Orders WHERE CustomerName = 'Rollback Customer'";
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    private static async Task<bool> ColumnExists(SqliteConnection connection, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Orders') WHERE name = $name";
        command.Parameters.AddWithValue("$name", column);

        return (long)(await command.ExecuteScalarAsync())! > 0;
    }
}
