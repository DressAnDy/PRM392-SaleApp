using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductListItemDto>> GetAllProductsAsync();
    Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductQueryDto query);
    Task<ProductDetailDto?> GetProductByIdAsync(int productId);
}
