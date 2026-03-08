using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated."));

    // ═══════════════════════════════════════════════════════════════════════════
    // User endpoints – the authenticated user operates on their own orders
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the current user's orders with optional filtering and paging.
    /// Query params: status, fromDate, toDate, sortBy (newest|oldest|total_asc|total_desc), page, pageSize.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryDto query)
    {
        var result = await _orderService.GetUserOrdersAsync(GetUserId(), query);
        return Ok(result);
    }

    /// <summary>
    /// Get the detail of one of the current user's orders.
    /// </summary>
    [HttpGet("my/{id:int}")]
    public async Task<IActionResult> GetMyOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdForUserAsync(id, GetUserId());
        if (order is null)
            return NotFound(new { message = "Order not found." });
        return Ok(order);
    }

    /// <summary>
    /// Place a new order. If 'items' is null or empty the order is created from the
    /// user's active cart, which is then cleared automatically.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return BadRequest(new { message = "Shipping address is required." });

        try
        {
            var order = await _orderService.CreateOrderAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetMyOrderById), new { id = order.OrderId }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel the current user's own order.
    /// Only allowed when status is Pending or Processing.
    /// </summary>
    [HttpPut("my/{id:int}/cancel")]
    public async Task<IActionResult> CancelMyOrder(int id)
    {
        try
        {
            var success = await _orderService.CancelOrderAsync(id, GetUserId());
            if (!success)
                return NotFound(new { message = "Order not found." });
            return Ok(new { message = "Order cancelled successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Admin endpoints – operate on any order
    // Note: protect with [Authorize(Roles = "Admin")] once role claims are added
    //       to the JWT token in AuthenticationService.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Admin] Get all orders with optional filtering and paging.
    /// Query params: status, userId, fromDate, toDate, sortBy, page, pageSize.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryDto query)
    {
        var result = await _orderService.GetOrdersAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Get any order by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order is null)
            return NotFound(new { message = "Order not found." });
        return Ok(order);
    }

    /// <summary>
    /// [Admin] Get orders filtered by a specific status, paged.
    /// Valid statuses: Pending, Processing, Shipped, Delivered, Cancelled, Refunded.
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetOrdersByStatus(
        string status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _orderService.GetOrdersByStatusAsync(status, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Update an order's status.
    /// Valid statuses: Pending, Processing, Shipped, Delivered, Cancelled, Refunded.
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderStatus))
            return BadRequest(new { message = "OrderStatus is required." });

        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, request);
            if (order is null)
                return NotFound(new { message = "Order not found." });
            return Ok(order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Hard-delete an order and all its line items (cleanup use only).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var success = await _orderService.DeleteOrderAsync(id);
        if (!success)
            return NotFound(new { message = "Order not found." });
        return Ok(new { message = "Order deleted successfully." });
    }
}
