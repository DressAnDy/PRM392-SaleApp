namespace SaleApp.Domain.Entities;

public class Cart
{
    public int CartId { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Active"; // Active/CheckedOut/Abandoned
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
