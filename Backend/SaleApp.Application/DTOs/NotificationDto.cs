namespace SaleApp.Application.DTOs;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}

public class CreateNotificationRequest
{
    public int? UserId { get; set; } // Null = broadcast to all users
    public string Type { get; set; } = string.Empty; // Order, Chat, Promotion, System
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
}

public class NotificationSummaryDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationDto> Notifications { get; set; } = new();
}
