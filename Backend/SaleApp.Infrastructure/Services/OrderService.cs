using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Domain.Entities;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly SaleAppDbContext _context;

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"
    };

    public OrderService(SaleAppDbContext context)
    {
        _context = context;
    }

    // ─── Mapping helpers ──────────────────────────────────────────────────────

    private static OrderItemDto MapItemToDto(OrderItem item) => new()
    {
        OrderItemId = item.OrderItemId,
        ProductId = item.ProductId,
        ProductNameSnapshot = item.ProductNameSnapshot,
        UnitPriceSnapshot = item.UnitPriceSnapshot,
        Quantity = item.Quantity
    };

    private static OrderSummaryDto MapToSummaryDto(Order order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.UserId,
        CustomerName = order.User?.Username ?? string.Empty,
        OrderStatus = order.OrderStatus,
        ShippingAddress = order.ShippingAddress,
        Subtotal = order.Subtotal,
        ShippingFee = order.ShippingFee,
        DiscountAmount = order.DiscountAmount,
        TotalItems = order.OrderItems.Sum(i => i.Quantity),
        CreatedAt = order.CreatedAt
    };

    private static OrderDetailDto MapToDetailDto(Order order)
    {
        var latestPayment = order.Payments?
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        return new OrderDetailDto
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            CustomerName = order.User?.Username ?? string.Empty,
            CustomerEmail = order.User?.Email,
            OrderStatus = order.OrderStatus,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            TotalItems = order.OrderItems.Sum(i => i.Quantity),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems.Select(MapItemToDto).ToList(),
            PaymentId = latestPayment?.PaymentId,
            PaymentStatus = latestPayment?.PaymentStatus,
            PaymentMethod = latestPayment?.Method ?? order.PaymentMethod
        };
    }

    // ─── Query helpers ────────────────────────────────────────────────────────

    private IQueryable<Order> BaseQuery() =>
        _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User)
            .Include(o => o.Payments);

    private static IQueryable<Order> ApplyFilter(IQueryable<Order> query, OrderQueryDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(o => o.OrderStatus == filter.Status);

        if (filter.UserId.HasValue)
            query = query.Where(o => o.UserId == filter.UserId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

        query = filter.SortBy.ToLower() switch
        {
            "oldest" => query.OrderBy(o => o.CreatedAt),
            "total_asc" => query.OrderBy(o => o.Subtotal + o.ShippingFee - o.DiscountAmount),
            "total_desc" => query.OrderByDescending(o => o.Subtotal + o.ShippingFee - o.DiscountAmount),
            _ => query.OrderByDescending(o => o.CreatedAt)   // newest (default)
        };

        return query;
    }

    private static async Task<PagedResultDto<OrderSummaryDto>> ToPagedResultAsync(
        IQueryable<Order> query, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<OrderSummaryDto>
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ─── Admin ────────────────────────────────────────────────────────────────

    public Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(OrderQueryDto query) =>
        ToPagedResultAsync(ApplyFilter(BaseQuery(), query), query.Page, query.PageSize);

    public async Task<OrderDetailDto?> GetOrderByIdAsync(int orderId)
    {
        var order = await BaseQuery().FirstOrDefaultAsync(o => o.OrderId == orderId);
        return order is null ? null : MapToDetailDto(order);
    }

    public Task<PagedResultDto<OrderSummaryDto>> GetOrdersByStatusAsync(string status, int page, int pageSize)
    {
        var query = BaseQuery()
            .Where(o => o.OrderStatus == status)
            .OrderByDescending(o => o.CreatedAt);

        return ToPagedResultAsync(query, page, pageSize);
    }

    public async Task<OrderDetailDto?> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        if (!ValidStatuses.Contains(request.OrderStatus))
            throw new ArgumentException(
                $"Invalid status '{request.OrderStatus}'. Valid values: {string.Join(", ", ValidStatuses)}.");

        var order = await BaseQuery().FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is null) return null;

        order.OrderStatus = request.OrderStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null) return false;

        _context.OrderItems.RemoveRange(order.OrderItems);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    // ─── User ─────────────────────────────────────────────────────────────────

    public Task<PagedResultDto<OrderSummaryDto>> GetUserOrdersAsync(int userId, OrderQueryDto query)
    {
        var q = ApplyFilter(BaseQuery().Where(o => o.UserId == userId), query);
        return ToPagedResultAsync(q, query.Page, query.PageSize);
    }

    public async Task<OrderDetailDto?> GetOrderByIdForUserAsync(int orderId, int userId)
    {
        var order = await BaseQuery()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        return order is null ? null : MapToDetailDto(order);
    }

    public async Task<OrderDetailDto> CreateOrderAsync(int userId, CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            throw new ArgumentException("Shipping address is required.");

        // ── 1. Load active cart with items and their products ─────────────────
        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart is null || !cart.CartItems.Any())
            throw new InvalidOperationException(
                "Your cart is empty. Add products before placing an order.");

        // ── 2. Convert each CartItem → OrderItem ──────────────────────────────
        // UnitPrice is the price that was locked when the item was added to cart,
        // so it is used as the snapshot — not the current product price.
        var orderItems = cart.CartItems.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,          // FK reference to Product
            ProductNameSnapshot = ci.Product?.ProductName ?? string.Empty,
            UnitPriceSnapshot = ci.UnitPrice,          // locked cart price
            Quantity = ci.Quantity
        }).ToList();

        var subtotal = orderItems.Sum(i => i.UnitPriceSnapshot * i.Quantity);

        // ── 3. Create the Order ───────────────────────────────────────────────
        var order = new Order
        {
            UserId = userId,
            OrderStatus = "Pending",
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Subtotal = subtotal,
            ShippingFee = request.ShippingFee,
            DiscountAmount = request.DiscountAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);

        // ── 4. Create a pending Payment record so VNPay can pick it up ─────────
        // Amount = Subtotal + ShippingFee - DiscountAmount (total user must pay)
        var payment = new Payment
        {
            Order = order,          // EF will resolve OrderId after SaveChanges
            Amount = subtotal + request.ShippingFee - request.DiscountAmount,
            Currency = "VND",
            Method = "VNPay",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _context.Payments.Add(payment);

        // ── 5. Mark cart as checked-out and remove its items ──────────────────
        _context.CartItems.RemoveRange(cart.CartItems);
        cart.Status = "CheckedOut";
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // ── 6. Reload with User + Payments for response mapping ───────────────
        var created = await BaseQuery().FirstAsync(o => o.OrderId == order.OrderId);
        return MapToDetailDto(created);
    }

    public async Task<CheckoutPreviewDto> GetCheckoutPreviewAsync(
        int userId, decimal shippingFee = 0, decimal discountAmount = 0)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart is null || !cart.CartItems.Any())
            return new CheckoutPreviewDto
            {
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                Message = "Your cart is empty."
            };

        var previewItems = cart.CartItems.Select(ci => new CheckoutPreviewItemDto
        {
            ProductId = ci.ProductId,
            ProductName = ci.Product?.ProductName ?? string.Empty,
            ImageUrl = ci.Product?.ImageUrl,
            UnitPrice = ci.UnitPrice,
            Quantity = ci.Quantity
        }).ToList();

        return new CheckoutPreviewDto
        {
            Items = previewItems,
            Subtotal = previewItems.Sum(i => i.LineTotal),
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            TotalItems = previewItems.Sum(i => i.Quantity)
        };
    }

    public async Task<bool> CancelOrderAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (order is null) return false;

        if (order.OrderStatus is "Shipped" or "Delivered")
            throw new InvalidOperationException(
                "Cannot cancel an order that has already been shipped or delivered.");

        if (order.OrderStatus is "Cancelled")
            throw new InvalidOperationException("Order is already cancelled.");

        order.OrderStatus = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
