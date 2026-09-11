using ClaudeAutomationDemo.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ClaudeAutomationDemo.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a private in-memory SQLite database. The schema is
/// built by running the actual EF Core migrations, so these tests exercise
/// <c>AddOrderProcessDate</c> rather than a model-shaped stand-in.
/// </summary>
public class OrdersApiFactory : WebApplicationFactory<Program>
{
    // Held open for the lifetime of the factory: an in-memory SQLite database is
    // discarded as soon as its last connection closes.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public OrdersApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            // Drop the app's SQLite-file registration and everything EF hangs off it.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(AppDbContext));

            foreach (var configuration in services
                         .Where(d => d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name
                                 .StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal))
                         .ToList())
            {
                services.Remove(configuration);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        return host;
    }

    /// <summary>Runs <paramref name="action"/> against the same database the API uses.</summary>
    public async Task WithDbContext(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
