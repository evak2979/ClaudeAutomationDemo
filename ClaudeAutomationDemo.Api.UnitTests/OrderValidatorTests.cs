using ClaudeAutomationDemo.Api.Models;
using ClaudeAutomationDemo.Api.Validation;
using Xunit;

namespace ClaudeAutomationDemo.Api.UnitTests;

/// <summary>
/// Covers the rule that a ProcessDate may sit at most
/// <see cref="OrderValidator.MaxProcessDateLeadDays"/> days ahead of now.
/// The clock is passed in, so the boundary can be pinned exactly.
/// </summary>
public class OrderValidatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private const string TooFarAhead =
        "ProcessDate must not be more than 7 days in the future.";

    private static Order ValidOrder(DateTimeOffset? processDate = null) => new()
    {
        CustomerName = "Ada Lovelace",
        Quantity = 1,
        UnitPrice = 10.00m,
        ProcessDate = processDate
    };

    [Fact]
    public void NullProcessDate_IsValid()
    {
        // An order that has not been processed yet, and every pre-existing order.
        Assert.Null(OrderValidator.Validate(ValidOrder(), Now));
    }

    [Fact]
    public void ProcessDateOfNow_IsValid()
    {
        Assert.Null(OrderValidator.Validate(ValidOrder(Now), Now));
    }

    [Theory]
    [InlineData(-1)]      // yesterday
    [InlineData(-30)]     // last month
    [InlineData(-3650)]   // ten years ago
    public void ProcessDateInThePast_IsValid(int daysFromNow)
    {
        // Only the future is capped — recording when something was already
        // processed has to keep working.
        var order = ValidOrder(Now.AddDays(daysFromNow));

        Assert.Null(OrderValidator.Validate(order, Now));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ProcessDateInsideTheWindow_IsValid(int daysAhead)
    {
        Assert.Null(OrderValidator.Validate(ValidOrder(Now.AddDays(daysAhead)), Now));
    }

    [Fact]
    public void ProcessDateExactlyOneWeekAhead_IsValid()
    {
        // The boundary itself is allowed: "up to one week" includes one week.
        var order = ValidOrder(Now.AddDays(OrderValidator.MaxProcessDateLeadDays));

        Assert.Null(OrderValidator.Validate(order, Now));
    }

    [Fact]
    public void ProcessDateJustPastOneWeek_IsRejected()
    {
        var order = ValidOrder(
            Now.AddDays(OrderValidator.MaxProcessDateLeadDays).AddTicks(1));

        Assert.Equal(TooFarAhead, OrderValidator.Validate(order, Now));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(30)]
    [InlineData(365)]
    public void ProcessDateBeyondOneWeek_IsRejected(int daysAhead)
    {
        var order = ValidOrder(Now.AddDays(daysAhead));

        Assert.Equal(TooFarAhead, OrderValidator.Validate(order, Now));
    }

    [Fact]
    public void Window_IsComparedAsAnInstant_NotWallClockText()
    {
        // Local wall clock reads 23 June — day 8 — but the +14:00 offset puts the
        // actual instant at 22 June 11:00 UTC, an hour inside the window.
        var insideWindow = new DateTimeOffset(2026, 6, 23, 1, 0, 0, TimeSpan.FromHours(14));
        Assert.True(insideWindow.UtcDateTime < Now.AddDays(7).UtcDateTime);

        Assert.Null(OrderValidator.Validate(ValidOrder(insideWindow), Now));
    }

    [Fact]
    public void NonPositiveQuantity_IsStillRejectedFirst()
    {
        // Pre-existing rule, and it takes precedence so the message stays stable.
        var order = ValidOrder(Now.AddDays(99));
        order.Quantity = 0;

        Assert.Equal("Quantity must be greater than zero.",
            OrderValidator.Validate(order, Now));
    }

    [Fact]
    public void NegativeQuantity_IsRejected()
    {
        var order = ValidOrder();
        order.Quantity = -5;

        Assert.Equal("Quantity must be greater than zero.",
            OrderValidator.Validate(order, Now));
    }

    [Fact]
    public void WindowMovesWithTheClock()
    {
        var processDate = Now.AddDays(5);

        // Same order, judged a week later: now out of the window.
        Assert.Null(OrderValidator.Validate(ValidOrder(processDate), Now));
        Assert.Equal(TooFarAhead,
            OrderValidator.Validate(ValidOrder(processDate), Now.AddDays(-3)));
    }
}
