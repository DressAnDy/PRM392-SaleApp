using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IOrderService
{
    // ─── Admin ────────────────────────────────────────────────────────────────

    /// <summary>Get all orders with filtering and paging (admin view).</summary>
    Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(OrderQueryDto query);

    /// <summary>Get any single order by ID (admin view).</summary>
    Task<OrderDetailDto?> GetOrderByIdAsync(int orderId);

    /// <summary>Get orders filtered by a specific status, paged (admin view).</summary>
    Task<PagedResultDto<OrderSummaryDto>> GetOrdersByStatusAsync(string status, int page, int pageSize);

    /// <summary>Update the status of an order (admin action).</summary>
    Task<OrderDetailDto?> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);

    /// <summary>Hard-delete an order and its items (admin cleanup).</summary>
    Task<bool> DeleteOrderAsync(int orderId);

    // ─── User ─────────────────────────────────────────────────────────────────

    /// <summary>Get the authenticated user's own orders with filtering and paging.</summary>
    Task<PagedResultDto<OrderSummaryDto>> GetUserOrdersAsync(int userId, OrderQueryDto query);

    /// <summary>Get a specific order that belongs to the authenticated user.</summary>
    Task<OrderDetailDto?> GetOrderByIdForUserAsync(int orderId, int userId);

    /// <summary>
    /// Place a new order by checking out everything in the user's active cart.
    /// The cart is cleared (status → CheckedOut) after a successful order.
    /// Throws <see cref="InvalidOperationException"/> when the cart is empty.
    /// </summary>
    Task<OrderDetailDto> CreateOrderAsync(int userId, CreateOrderRequest request);

    /// <summary>Cancel an order that belongs to the user (Pending or Processing only).</summary>
    Task<bool> CancelOrderAsync(int orderId, int userId);

    /// <summary>
    /// Preview the checkout totals from the user's active cart without placing the order.
    /// </summary>
    Task<CheckoutPreviewDto> GetCheckoutPreviewAsync(int userId, decimal shippingFee = 0, decimal discountAmount = 0);
}
