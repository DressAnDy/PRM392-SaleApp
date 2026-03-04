namespace SaleApp.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}
