using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Domain.Entities;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly SaleAppDbContext _context;

    public CartService(SaleAppDbContext context)
    {
        _context = context;
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static CartDto MapToDto(Cart cart) => new CartDto
    {
        CartId  = cart.CartId,
        UserId  = cart.UserId,
        Items   = cart.CartItems.Select(ci => new CartItemDto
        {
            CartItemId  = ci.CartItemId,
            ProductId   = ci.ProductId,
            ProductName = ci.Product.ProductName,
            ImageUrl    = ci.Product.ImageUrl,
            Quantity    = ci.Quantity,
            UnitPrice   = ci.UnitPrice
        }).ToList()
    };

    private async Task<Cart?> GetActiveCartAsync(int userId) =>
        await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

    // ─── interface implementations ────────────────────────────────────────────

    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        var cart = await GetActiveCartAsync(userId);
        return cart == null ? null : MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request)
    {
        // Get or create active cart
        var cart = await GetActiveCartAsync(userId);
        if (cart == null)
        {
            cart = new Cart { UserId = userId, Status = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            // Reload with includes
            cart = (await GetActiveCartAsync(userId))!;
        }

        // Fetch product for current price
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive)
            ?? throw new InvalidOperationException("Product not found or inactive.");

        // Check if item already in cart
        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            _context.CartItems.Update(existingItem);
        }
        else
        {
            var newItem = new CartItem
            {
                CartId    = cart.CartId,
                ProductId = request.ProductId,
                Quantity  = request.Quantity,
                UnitPrice = product.CurrentPrice
            };
            _context.CartItems.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reload full cart
        cart = (await GetActiveCartAsync(userId))!;
        return MapToDto(cart);
    }

    public async Task<CartDto?> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequest request)
    {
        var cart = await GetActiveCartAsync(userId);
        if (cart == null) return null;

        var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
        if (item == null) return null;

        if (request.Quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
            _context.CartItems.Update(item);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        cart = (await GetActiveCartAsync(userId))!;
        return MapToDto(cart);
    }

    public async Task<CartDto?> RemoveCartItemAsync(int userId, int cartItemId)
    {
        var cart = await GetActiveCartAsync(userId);
        if (cart == null) return null;

        var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
        if (item == null) return null;

        _context.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        cart = (await GetActiveCartAsync(userId))!;
        return MapToDto(cart);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await GetActiveCartAsync(userId);
        if (cart == null) return false;

        _context.CartItems.RemoveRange(cart.CartItems);
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
