using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Domain.Entities;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly SaleAppDbContext _context;

    public NotificationService(SaleAppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request)
    {
        if (request.UserId == null)
        {
            throw new ArgumentException("UserId is required for single notification");
        }

        var notification = new Notification
        {
            UserId = request.UserId.Value,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            DeepLink = request.DeepLink,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    public async Task<List<NotificationDto>> CreateBulkNotificationsAsync(List<int> userIds, string type, string title, string message, string? deepLink = null)
    {
        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            DeepLink = deepLink,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<NotificationDto>> BroadcastNotificationAsync(string type, string title, string message, string? deepLink = null)
    {
        // Get all active user IDs
        var userIds = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => u.UserId)
            .ToListAsync();

        return await CreateBulkNotificationsAsync(userIds, type, title, message, deepLink);
    }

    public async Task<NotificationSummaryDto> GetUserNotificationsAsync(int userId, int skip = 0, int take = 20, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var totalCount = await query.CountAsync();
        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new NotificationSummaryDto
        {
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Notifications = notifications.Select(MapToDto).ToList()
        };
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

        return notification != null ? MapToDto(notification) : null;
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

        if (notification == null || notification.ReadAt != null)
            return false;

        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return unreadNotifications.Count;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteAllNotificationsAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();
        return notifications.Count;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .CountAsync();
    }

    private NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            DeepLink = notification.DeepLink,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
