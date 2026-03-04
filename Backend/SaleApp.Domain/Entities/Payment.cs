namespace SaleApp.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Method { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order Order { get; set; } = null!;
}
