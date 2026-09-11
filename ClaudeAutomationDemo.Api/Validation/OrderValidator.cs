using ClaudeAutomationDemo.Api.Models;

namespace ClaudeAutomationDemo.Api.Validation;

/// <summary>
/// Validation rules for incoming orders. Kept as a pure function of the order and
/// the current instant so the time-sensitive rules can be tested without waiting
/// for the clock; the endpoint is still what turns a failure into a 400.
/// </summary>
public static class OrderValidator
{
    /// <summary>How far ahead of "now" a <see cref="Order.ProcessDate"/> may be set.</summary>
    public const int MaxProcessDateLeadDays = 7;

    /// <summary>
    /// Returns the first validation failure, or null when <paramref name="order"/> is acceptable.
    /// </summary>
    public static string? Validate(Order order, DateTimeOffset now)
    {
        if (order.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        // A null ProcessDate stays valid: the order simply has not been processed.
        // Dates in the past stay valid too — only the future is capped.
        if (order.ProcessDate is { } processDate
            && processDate > now.AddDays(MaxProcessDateLeadDays))
        {
            return $"ProcessDate must not be more than {MaxProcessDateLeadDays} days in the future.";
        }

        return null;
    }
}
