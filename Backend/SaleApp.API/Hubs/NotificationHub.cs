using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using System.Security.Claims;

namespace SaleApp.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        
        if (userId.HasValue)
        {
            // Join user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_notifications_{userId.Value}");
            
            // Send current unread count
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
            await Clients.Caller.SendAsync("UnreadCountUpdated", unreadCount);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_notifications_{userId.Value}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client requests to mark notification as read
    public async Task MarkNotificationAsRead(int notificationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        // Get notification to verify ownership
        var notification = await _notificationService.GetNotificationByIdAsync(notificationId);
        if (notification == null)
        {
            throw new HubException("Notification not found");
        }

        if (notification.UserId != userId.Value)
        {
            throw new HubException("Unauthorized");
        }

        // Mark as read
        await _notificationService.MarkAsReadAsync(notificationId);

        // Send updated unread count
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
        await Clients.Caller.SendAsync("UnreadCountUpdated", unreadCount);
    }

    // Client requests to mark all notifications as read
    public async Task MarkAllAsRead()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        await _notificationService.MarkAllAsReadAsync(userId.Value);

        // Send updated unread count (should be 0)
        await Clients.Caller.SendAsync("UnreadCountUpdated", 0);
    }

    // Get current unread count
    public async Task GetUnreadCount()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
        await Clients.Caller.SendAsync("UnreadCountUpdated", unreadCount);
    }

    // Helper method
    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }
}
