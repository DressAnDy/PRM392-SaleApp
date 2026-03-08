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
    public string? PaymentMethod { get; set; }
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
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Request to place a new order. If <see cref="Items"/> is null or empty,
/// the order will be created from the user's active cart.
/// </summary>
public class CreateOrderRequest
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public List<CreateOrderItemRequest>? Items { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string OrderStatus { get; set; } = string.Empty;
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
