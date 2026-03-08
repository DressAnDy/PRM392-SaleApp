using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface INotificationService
{
    // Create notifications
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request);
    Task<List<NotificationDto>> CreateBulkNotificationsAsync(List<int> userIds, string type, string title, string message, string? deepLink = null);
    Task<List<NotificationDto>> BroadcastNotificationAsync(string type, string title, string message, string? deepLink = null);
    
    // Get notifications
    Task<NotificationSummaryDto> GetUserNotificationsAsync(int userId, int skip = 0, int take = 20, bool unreadOnly = false);
    Task<NotificationDto?> GetNotificationByIdAsync(int notificationId);
    
    // Mark as read
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<int> MarkAllAsReadAsync(int userId);
    
    // Delete
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<int> DeleteAllNotificationsAsync(int userId);
    
    // Statistics
    Task<int> GetUnreadCountAsync(int userId);
}
