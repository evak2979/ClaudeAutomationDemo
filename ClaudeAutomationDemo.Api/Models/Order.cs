namespace ClaudeAutomationDemo.Api.Models;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the order was processed. Null for orders that have not been
    /// processed yet, and for orders created before this field existed.
    /// </summary>
    public DateTimeOffset? ProcessDate { get; set; }
}
