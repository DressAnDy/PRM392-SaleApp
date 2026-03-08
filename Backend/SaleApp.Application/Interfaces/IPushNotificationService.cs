using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

/// <summary>
/// Service for sending push notifications (FCM/APNs)
/// </summary>
public interface IPushNotificationService
{
    // Device token management
    Task<DeviceTokenDto> RegisterDeviceTokenAsync(int userId, RegisterDeviceTokenRequest request);
    Task<bool> RemoveDeviceTokenAsync(int userId, string token);
    Task<List<DeviceTokenDto>> GetUserDeviceTokensAsync(int userId);
    
    // Send push notifications
    Task<bool> SendPushNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null);
    Task<int> SendPushNotificationToMultipleUsersAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null);
    Task<int> BroadcastPushNotificationAsync(string title, string body, Dictionary<string, string>? data = null);
}
