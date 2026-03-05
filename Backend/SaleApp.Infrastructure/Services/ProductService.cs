using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly SaleAppDbContext _context;

    public ProductService(SaleAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductListItemDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Feedbacks)
            .Include(p => p.OrderItems)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var items = products.Select(p =>
        {
            var ratings = p.Feedbacks.Where(f => f.Rating.HasValue).Select(f => f.Rating!.Value).ToList();
            return new ProductListItemDto
            {
                ProductId    = p.ProductId,
                ProductName  = p.ProductName,
                ImageUrl     = p.ImageUrl,
                CurrentPrice = p.CurrentPrice,
                Description  = p.Description,
                CategoryName = p.Category.CategoryName,
                AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 0,
                TotalReviews  = ratings.Count,
                TotalSold     = p.OrderItems.Sum(oi => oi.Quantity)
            };
        }).ToList();

        return items;
    }

    public async Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductQueryDto query)
    {
        var q = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Feedbacks)
            .Include(p => p.OrderItems)
            .AsQueryable();

        // Filter by category
        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        // Filter by price range
        if (query.MinPrice.HasValue)
            q = q.Where(p => p.CurrentPrice >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.CurrentPrice <= query.MaxPrice.Value);

        // Filter by rating (average)
        if (query.MinRating.HasValue)
        {
            q = q.Where(p =>
                p.Feedbacks.Any() &&
                p.Feedbacks.Where(f => f.Rating.HasValue).Average(f => (double)f.Rating!.Value) >= query.MinRating.Value);
        }

        // Sort
        q = query.SortBy switch
        {
            "price_asc"   => q.OrderBy(p => p.CurrentPrice),
            "price_desc"  => q.OrderByDescending(p => p.CurrentPrice),
            "popularity"  => q.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)),
            _             => q.OrderByDescending(p => p.CreatedAt) // newest
        };

        var totalCount = await q.CountAsync();

        var products = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = products.Select(p =>
        {
            var ratings = p.Feedbacks.Where(f => f.Rating.HasValue).Select(f => f.Rating!.Value).ToList();
            return new ProductListItemDto
            {
                ProductId    = p.ProductId,
                ProductName  = p.ProductName,
                ImageUrl     = p.ImageUrl,
                CurrentPrice = p.CurrentPrice,
                Description  = p.Description,
                CategoryName = p.Category.CategoryName,
                AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 0,
                TotalReviews  = ratings.Count,
                TotalSold     = p.OrderItems.Sum(oi => oi.Quantity)
            };
        }).ToList();

        return new PagedResultDto<ProductListItemDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = query.Page,
            PageSize   = query.PageSize
        };
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int productId)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Feedbacks)
                .ThenInclude(f => f.User)
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

        if (product == null) return null;

        var ratings = product.Feedbacks.Where(f => f.Rating.HasValue).Select(f => f.Rating!.Value).ToList();

        return new ProductDetailDto
        {
            ProductId              = product.ProductId,
            ProductName            = product.ProductName,
            Description            = product.Description,
            TechnicalSpecifications = product.TechnicalSpecifications,
            CurrentPrice           = product.CurrentPrice,
            ImageUrl               = product.ImageUrl,
            CategoryId             = product.CategoryId,
            CategoryName           = product.Category.CategoryName,
            AverageRating          = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 0,
            TotalReviews           = ratings.Count,
            TotalSold              = product.OrderItems.Sum(oi => oi.Quantity),
            CreatedAt              = product.CreatedAt,
            Feedbacks = product.Feedbacks.OrderByDescending(f => f.CreatedAt).Select(f => new FeedbackDto
            {
                FeedbackId        = f.FeedbackId,
                Username          = f.User.Username,
                Rating            = f.Rating,
                Comment           = f.Comment,
                IsVerifiedPurchase = f.IsVerifiedPurchase,
                CreatedAt         = f.CreatedAt
            }).ToList()
        };
    }
}
