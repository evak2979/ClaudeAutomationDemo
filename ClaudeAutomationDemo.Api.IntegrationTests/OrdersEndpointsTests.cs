using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClaudeAutomationDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaudeAutomationDemo.Api.IntegrationTests;

/// <summary>
/// End-to-end coverage of <c>/orders</c> over the real HTTP pipeline and a real
/// (migrated) SQLite database, focused on the new <c>processDate</c> field and on
/// the existing behaviour it had to leave untouched.
/// </summary>
public class OrdersEndpointsTests : IClassFixture<OrdersApiFactory>
{
    private readonly OrdersApiFactory _factory;
    private readonly HttpClient _client;

    public OrdersEndpointsTests(OrdersApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithoutProcessDate_CreatesUnprocessedOrder()
    {
        // The payload a pre-existing client sends: no processDate at all.
        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "Legacy Client",
            quantity = 2,
            unitPrice = 10.50m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);
        Assert.Null(created.ProcessDate);
        Assert.Equal("Legacy Client", created.CustomerName);

        await _factory.WithDbContext(async db =>
        {
            var persisted = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == created.Id);
            Assert.Null(persisted.ProcessDate);
        });
    }

    [Fact]
    public async Task Post_WithProcessDate_PersistsAndReturnsIt()
    {
        var processedAt = new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "Processed Customer",
            quantity = 1,
            unitPrice = 42.00m,
            processDate = processedAt
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);
        Assert.Equal(processedAt, created.ProcessDate);

        await _factory.WithDbContext(async db =>
        {
            var persisted = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == created.Id);
            Assert.Equal(processedAt, persisted.ProcessDate);
        });
    }

    [Fact]
    public async Task GetById_ReturnsProcessDate_ForProcessedAndUnprocessedOrders()
    {
        var processedAt = new DateTimeOffset(2026, 6, 1, 7, 15, 0, TimeSpan.Zero);

        var processed = await (await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "Get Processed",
            quantity = 1,
            unitPrice = 1.00m,
            processDate = processedAt
        })).Content.ReadFromJsonAsync<Order>();

        var unprocessed = await (await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "Get Unprocessed",
            quantity = 1,
            unitPrice = 1.00m
        })).Content.ReadFromJsonAsync<Order>();

        Assert.NotNull(processed);
        Assert.NotNull(unprocessed);

        var fetchedProcessed = await _client.GetFromJsonAsync<Order>($"/orders/{processed.Id}");
        var fetchedUnprocessed = await _client.GetFromJsonAsync<Order>($"/orders/{unprocessed.Id}");

        Assert.Equal(processedAt, fetchedProcessed?.ProcessDate);
        Assert.Null(fetchedUnprocessed?.ProcessDate);
    }

    [Fact]
    public async Task GetAll_ExposesProcessDateOnEveryOrder()
    {
        await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "List Me",
            quantity = 1,
            unitPrice = 3.00m,
            processDate = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero)
        });

        var response = await _client.GetAsync("/orders");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.NotEmpty(document.RootElement.EnumerateArray());
        foreach (var order in document.RootElement.EnumerateArray())
        {
            // Present on every order, null where the order is not processed yet.
            Assert.True(order.TryGetProperty("processDate", out _));
        }
    }

    [Fact]
    public async Task PreExistingRow_WrittenWithoutTheColumn_ReadsBackAsNull()
    {
        // Simulates a row that was already in the database before this migration:
        // inserted naming only the original columns, leaving ProcessDate untouched.
        const int legacyId = 9001;
        var legacyCreatedAt = new DateTimeOffset(2025, 11, 30, 16, 45, 0, TimeSpan.Zero);

        await _factory.WithDbContext(async db =>
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Orders (Id, CustomerName, Quantity, UnitPrice, CreatedAt)
                VALUES ({0}, {1}, {2}, {3}, {4})
                """,
                legacyId, "Pre-Migration Customer", 7, 55.25m, legacyCreatedAt));

        var fetched = await _client.GetFromJsonAsync<Order>($"/orders/{legacyId}");

        Assert.NotNull(fetched);
        Assert.Null(fetched.ProcessDate);
        // Guards against the raw insert silently writing an unreadable value.
        Assert.Equal("Pre-Migration Customer", fetched.CustomerName);
        Assert.Equal(legacyCreatedAt, fetched.CreatedAt);
        Assert.Equal(55.25m, fetched.UnitPrice);
    }

    [Fact]
    public async Task Post_WithNonPositiveQuantity_StillReturnsProblem()
    {
        // Existing validation must be unaffected by the new field.
        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerName = "Invalid",
            quantity = 0,
            unitPrice = 1.00m,
            processDate = DateTimeOffset.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Quantity must be greater than zero.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithMalformedProcessDate_ReturnsBadRequest()
    {
        var request = new StringContent(
            """{"customerName":"Bad Date","quantity":1,"unitPrice":1.00,"processDate":"not-a-date"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownOrder_StillReturnsNotFound()
    {
        var response = await _client.GetAsync("/orders/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
