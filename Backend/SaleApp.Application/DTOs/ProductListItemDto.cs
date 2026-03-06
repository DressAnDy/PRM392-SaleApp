namespace SaleApp.Application.DTOs;

public class ProductListItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalSold { get; set; }
}
