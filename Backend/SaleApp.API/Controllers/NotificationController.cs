using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SaleApp.API.Hubs;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using System.Security.Claims;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _notificationHubContext;

    public NotificationController(
        INotificationService notificationService,
        IHubContext<NotificationHub> notificationHubContext)
    {
        _notificationService = notificationService;
        _notificationHubContext = notificationHubContext;
    }

    // GET: api/Notification
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 20, 
        [FromQuery] bool unreadOnly = false)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, skip, take, unreadOnly);
        return Ok(result);
    }

    // GET: api/Notification/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        
        if (notification == null)
            return NotFound(new { message = "Notification not found" });

        // Verify ownership
        var userId = GetUserId();
        if (notification.UserId != userId)
            return Forbid();

        return Ok(notification);
    }

    // POST: api/Notification (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin,Shop")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required" });

        if (request.UserId.HasValue)
        {
            // Send to specific user
            var notification = await _notificationService.CreateNotificationAsync(request);
            
            // Push realtime via SignalR
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification);

            // Update unread count
            var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("UnreadCountUpdated", unreadCount);

            return CreatedAtAction(nameof(GetNotification), new { id = notification.NotificationId }, notification);
        }
        else
        {
            // Broadcast to all users
            var notifications = await _notificationService.BroadcastNotificationAsync(
                request.Type, 
                request.Title ?? "", 
                request.Message, 
                request.DeepLink);

            // Push realtime to all connected users
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

            return Ok(new { message = $"Broadcast sent to {notifications.Count} users", count = notifications.Count });
        }
    }

    // PUT: api/Notification/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        
        if (notification == null)
            return NotFound(new { message = "Notification not found" });

        // Verify ownership
        var userId = GetUserId();
        if (notification.UserId != userId)
            return Forbid();

        var result = await _notificationService.MarkAsReadAsync(id);
        
        if (!result)
            return BadRequest(new { message = "Notification already read" });

        // Update unread count via SignalR
        var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);
        await _notificationHubContext.Clients
            .Group($"user_notifications_{notification.UserId}")
            .SendAsync("UnreadCountUpdated", unreadCount);

        return NoContent();
    }

    // PUT: api/Notification/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notificationService.MarkAllAsReadAsync(userId.Value);

        // Update unread count via SignalR
        await _notificationHubContext.Clients
            .Group($"user_notifications_{userId.Value}")
            .SendAsync("UnreadCountUpdated", 0);

        return Ok(new { message = $"Marked {count} notifications as read", count });
    }

    // DELETE: api/Notification/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        
        if (notification == null)
            return NotFound(new { message = "Notification not found" });

        // Verify ownership
        var userId = GetUserId();
        if (notification.UserId != userId)
            return Forbid();

        var result = await _notificationService.DeleteNotificationAsync(id);
        
        if (!result)
            return NotFound(new { message = "Notification not found" });

        // Update unread count if it was unread
        if (!notification.IsRead)
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);
            await _notificationHubContext.Clients
                .Group($"user_notifications_{notification.UserId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }

        return NoContent();
    }

    // DELETE: api/Notification/clear-all
    [HttpDelete("clear-all")]
    public async Task<IActionResult> DeleteAllNotifications()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notificationService.DeleteAllNotificationsAsync(userId.Value);

        // Update unread count via SignalR
        await _notificationHubContext.Clients
            .Group($"user_notifications_{userId.Value}")
            .SendAsync("UnreadCountUpdated", 0);

        return Ok(new { message = $"Deleted {count} notifications", count });
    }

    // GET: api/Notification/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(new { unreadCount = count });
    }

    // Helper method
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }
}
