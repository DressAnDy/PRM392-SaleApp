using Microsoft.AspNetCore.SignalR;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Hubs;

/// <summary>
/// Helper class to send notifications from other services (Order, Chat, etc.)
/// </summary>
public class NotificationHelper
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _notificationHubContext;

    public NotificationHelper(
        INotificationService notificationService,
        IHubContext<NotificationHub> notificationHubContext)
    {
        _notificationService = notificationService;
        _notificationHubContext = notificationHubContext;
    }

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    public async Task SendNotificationToUserAsync(int userId, string type, string title, string message, string? deepLink = null)
    {
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            DeepLink = deepLink
        };

        var notification = await _notificationService.CreateNotificationAsync(request);

        // Push realtime via SignalR
        await _notificationHubContext.Clients
            .Group($"user_notifications_{userId}")
            .SendAsync("ReceiveNotification", notification);

        // Update unread count
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
        await _notificationHubContext.Clients
            .Group($"user_notifications_{userId}")
            .SendAsync("UnreadCountUpdated", unreadCount);
    }

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    public async Task SendNotificationToUsersAsync(List<int> userIds, string type, string title, string message, string? deepLink = null)
    {
        var notifications = await _notificationService.CreateBulkNotificationsAsync(userIds, type, title, message, deepLink);

        foreach (var notification in notifications)
        {
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification);

            var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }
    }

    /// <summary>
    /// Broadcast notification to all users
    /// </summary>
    public async Task BroadcastNotificationAsync(string type, string title, string message, string? deepLink = null)
    {
        var notifications = await _notificationService.BroadcastNotificationAsync(type, title, message, deepLink);

        foreach (var notification in notifications)
        {
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification);

            var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }
    }

    /// <summary>
    /// Send order notification
    /// </summary>
    public async Task SendOrderNotificationAsync(int userId, int orderId, string status, string message)
    {
        var deepLink = $"saleapp://order/{orderId}";
        await SendNotificationToUserAsync(userId, "Order", $"Order #{orderId}", message, deepLink);
    }

    /// <summary>
    /// Send chat notification
    /// </summary>
    public async Task SendChatNotificationAsync(int userId, int conversationId, string message)
    {
        var deepLink = $"saleapp://chat/{conversationId}";
        await SendNotificationToUserAsync(userId, "Chat", "New message", message, deepLink);
    }

    /// <summary>
    /// Send promotion notification
    /// </summary>
    public async Task SendPromotionNotificationAsync(string title, string message, string? deepLink = null)
    {
        await BroadcastNotificationAsync("Promotion", title, message, deepLink);
    }
}
