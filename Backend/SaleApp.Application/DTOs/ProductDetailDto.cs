namespace SaleApp.Application.DTOs;

public class ProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TechnicalSpecifications { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalSold { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FeedbackDto> Feedbacks { get; set; } = new();
}

public class FeedbackDto
{
    public Guid FeedbackId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public DateTime CreatedAt { get; set; }
}
