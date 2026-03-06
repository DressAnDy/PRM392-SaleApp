using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(int userId);
    Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request);
    Task<CartDto?> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequest request);
    Task<CartDto?> RemoveCartItemAsync(int userId, int cartItemId);
    Task<bool> ClearCartAsync(int userId);
}
