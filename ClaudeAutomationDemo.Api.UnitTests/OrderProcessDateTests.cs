using System.Text.Json;
using ClaudeAutomationDemo.Api.Models;
using Xunit;

namespace ClaudeAutomationDemo.Api.UnitTests;

/// <summary>
/// Covers the <see cref="Order.ProcessDate"/> field and, above all, the
/// promise that adding it did not break the existing order contract:
/// a payload written before the field existed still binds correctly.
/// </summary>
public class OrderProcessDateTests
{
    // Minimal APIs serialise with the "Web" defaults (camelCase, case-insensitive
    // binding), so the tests below use the same options the endpoints do.
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void NewOrder_DefaultsProcessDateToNull()
    {
        var order = new Order();

        Assert.Null(order.ProcessDate);
    }

    [Fact]
    public void ProcessDate_CanBeSetAndReadBack()
    {
        var processedAt = new DateTimeOffset(2026, 3, 4, 9, 30, 0, TimeSpan.Zero);

        var order = new Order { ProcessDate = processedAt };

        Assert.Equal(processedAt, order.ProcessDate);
    }

    [Fact]
    public void LegacyPayload_WithoutProcessDate_BindsWithNullProcessDate()
    {
        // Exactly the JSON a client written against the pre-ProcessDate API sends.
        const string legacyJson = """
        {
          "customerName": "Ada Lovelace",
          "quantity": 3,
          "unitPrice": 9.99,
          "createdAt": "2026-01-15T10:00:00+00:00"
        }
        """;

        var order = JsonSerializer.Deserialize<Order>(legacyJson, WebJson);

        Assert.NotNull(order);
        Assert.Null(order.ProcessDate);
        Assert.Equal("Ada Lovelace", order.CustomerName);
        Assert.Equal(3, order.Quantity);
        Assert.Equal(9.99m, order.UnitPrice);
        Assert.Equal(
            new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            order.CreatedAt);
    }

    [Fact]
    public void Payload_WithExplicitNullProcessDate_BindsToNull()
    {
        const string json = """
        {
          "customerName": "Grace Hopper",
          "quantity": 1,
          "unitPrice": 5.00,
          "processDate": null
        }
        """;

        var order = JsonSerializer.Deserialize<Order>(json, WebJson);

        Assert.NotNull(order);
        Assert.Null(order.ProcessDate);
    }

    [Fact]
    public void Payload_WithProcessDate_BindsToThatInstant()
    {
        const string json = """
        {
          "customerName": "Grace Hopper",
          "quantity": 1,
          "unitPrice": 5.00,
          "processDate": "2026-02-20T14:45:00+00:00"
        }
        """;

        var order = JsonSerializer.Deserialize<Order>(json, WebJson);

        Assert.NotNull(order);
        Assert.Equal(
            new DateTimeOffset(2026, 2, 20, 14, 45, 0, TimeSpan.Zero),
            order.ProcessDate);
    }

    [Fact]
    public void Serialising_UnprocessedOrder_EmitsProcessDateAsNull()
    {
        // The field is always present in responses so consumers can rely on it,
        // rather than having to distinguish "absent" from "not processed".
        var order = new Order { CustomerName = "Ada Lovelace", Quantity = 1, UnitPrice = 1m };

        var json = JsonSerializer.Serialize(order, WebJson);

        Assert.Contains("\"processDate\":null", json);
    }

    [Fact]
    public void Serialising_ProcessedOrder_RoundTripsTheValue()
    {
        var order = new Order
        {
            CustomerName = "Ada Lovelace",
            Quantity = 2,
            UnitPrice = 12.50m,
            ProcessDate = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero)
        };

        var roundTripped = JsonSerializer.Deserialize<Order>(
            JsonSerializer.Serialize(order, WebJson), WebJson);

        Assert.NotNull(roundTripped);
        Assert.Equal(order.ProcessDate, roundTripped.ProcessDate);
        Assert.Equal(order.CustomerName, roundTripped.CustomerName);
        Assert.Equal(order.UnitPrice, roundTripped.UnitPrice);
    }
}
