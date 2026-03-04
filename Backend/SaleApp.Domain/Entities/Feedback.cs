namespace SaleApp.Domain.Entities;

public class Feedback
{
    public Guid FeedbackId { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
