using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        return int.Parse(value);
    }

    /// <summary>
    /// Get the current user's active cart.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _cartService.GetCartByUserIdAsync(GetUserId());
        if (cart == null)
            return Ok(new CartDto { UserId = GetUserId() }); // return empty cart
        return Ok(cart);
    }

    /// <summary>
    /// Add a product to the cart (or increase quantity if already present).
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        try
        {
            var cart = await _cartService.AddToCartAsync(GetUserId(), request);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update the quantity of a specific cart item. Set quantity to 0 to remove.
    /// </summary>
    [HttpPut("items/{cartItemId:int}")]
    public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        var cart = await _cartService.UpdateCartItemAsync(GetUserId(), cartItemId, request);
        if (cart == null)
            return NotFound(new { message = "Cart or item not found." });
        return Ok(cart);
    }

    /// <summary>
    /// Remove a specific item from the cart.
    /// </summary>
    [HttpDelete("items/{cartItemId:int}")]
    public async Task<IActionResult> RemoveCartItem(int cartItemId)
    {
        var cart = await _cartService.RemoveCartItemAsync(GetUserId(), cartItemId);
        if (cart == null)
            return NotFound(new { message = "Cart or item not found." });
        return Ok(cart);
    }

    /// <summary>
    /// Clear all items from the cart.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var success = await _cartService.ClearCartAsync(GetUserId());
        if (!success)
            return NotFound(new { message = "No active cart found." });
        return Ok(new { message = "Cart cleared successfully." });
    }
}
