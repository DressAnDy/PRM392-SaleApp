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
        PaymentMethod = order.PaymentMethod,
        ShippingAddress = order.ShippingAddress,
        Subtotal = order.Subtotal,
        ShippingFee = order.ShippingFee,
        DiscountAmount = order.DiscountAmount,
        TotalItems = order.OrderItems.Sum(i => i.Quantity),
        CreatedAt = order.CreatedAt
    };

    private static OrderDetailDto MapToDetailDto(Order order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.UserId,
        CustomerName = order.User?.Username ?? string.Empty,
        CustomerEmail = order.User?.Email,
        OrderStatus = order.OrderStatus,
        PaymentMethod = order.PaymentMethod,
        ShippingAddress = order.ShippingAddress,
        BillingAddress = order.BillingAddress,
        Subtotal = order.Subtotal,
        ShippingFee = order.ShippingFee,
        DiscountAmount = order.DiscountAmount,
        TotalItems = order.OrderItems.Sum(i => i.Quantity),
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.OrderItems.Select(MapItemToDto).ToList()
    };

    // ─── Query helpers ────────────────────────────────────────────────────────

    private IQueryable<Order> BaseQuery() =>
        _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User);

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

        // Determine line items: from explicit list or from the active cart
        List<(int ProductId, int Quantity)> lineItems;

        bool fromCart = request.Items is null || request.Items.Count == 0;

        if (!fromCart)
        {
            var explicitItems = request.Items!;
            if (explicitItems.Any(i => i.Quantity <= 0))
                throw new ArgumentException("All item quantities must be greater than 0.");

            lineItems = explicitItems.Select(i => (i.ProductId, i.Quantity)).ToList();
        }
        else
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart is null || !cart.CartItems.Any())
                throw new InvalidOperationException("No items provided and your cart is empty.");

            lineItems = cart.CartItems.Select(ci => (ci.ProductId, ci.Quantity)).ToList();
        }

        // Validate products
        var productIds = lineItems.Select(l => l.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && p.IsActive)
            .ToListAsync();

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more products were not found or are inactive.");

        // Build order items
        var orderItems = lineItems.Select(l =>
        {
            var product = products.First(p => p.ProductId == l.ProductId);
            return new OrderItem
            {
                ProductId = l.ProductId,
                ProductNameSnapshot = product.ProductName,
                UnitPriceSnapshot = product.CurrentPrice,
                Quantity = l.Quantity
            };
        }).ToList();

        var subtotal = orderItems.Sum(i => i.UnitPriceSnapshot * i.Quantity);

        var order = new Order
        {
            UserId = userId,
            OrderStatus = "Pending",
            PaymentMethod = request.PaymentMethod,
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

        // Clear the cart after checkout
        if (fromCart)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart is not null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.Status = "CheckedOut";
                cart.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(o => o.OrderId == order.OrderId);
        return MapToDetailDto(created);
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
