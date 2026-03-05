namespace SaleApp.Application.DTOs;

public class ProductQueryDto
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }

    // price_asc | price_desc | popularity | newest
    public string SortBy { get; set; } = "newest";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
