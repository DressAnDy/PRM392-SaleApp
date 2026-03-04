namespace SaleApp.Domain.Entities;

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
