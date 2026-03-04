namespace SaleApp.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string OrderStatus { get; set; } = "Pending";
    public string? PaymentMethod { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }
    public decimal Subtotal { get; set; } = 0;
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment>? Payments { get; set; } = new List<Payment>();
}
