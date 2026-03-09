namespace SaleApp.Application.DTOs;

public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int? ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPriceSnapshot * Quantity;
}

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount => Subtotal + ShippingFee - DiscountAmount;
    public int TotalItems { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailDto : OrderSummaryDto
{
    public string? BillingAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();

    // Payment info — populated after order creation
    public int? PaymentId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Request to place a new order. Items are always read from the user's active cart.
/// </summary>
public class CreateOrderRequest
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
}

public class UpdateOrderStatusRequest
{
    public string OrderStatus { get; set; } = string.Empty;
}

/// <summary>
/// Preview item — mirrors CartItemDto but uses the locked price from CartItem.
/// </summary>
public class CheckoutPreviewItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// Breakdown returned by GET /api/orders/checkout-preview before the user confirms.
/// </summary>
public class CheckoutPreviewDto
{
    public List<CheckoutPreviewItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>Subtotal + ShippingFee - DiscountAmount</summary>
    public decimal TotalAmount => Subtotal + ShippingFee - DiscountAmount;
    public int TotalItems { get; set; }
    public string? Message { get; set; }
}

public class OrderQueryDto
{
    public string? Status { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>newest | oldest | total_asc | total_desc</summary>
    public string SortBy { get; set; } = "newest";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
