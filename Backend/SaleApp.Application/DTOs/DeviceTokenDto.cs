namespace SaleApp.Application.DTOs;

public class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // Android, iOS, Web
    public string? DeviceName { get; set; }
}

public class DeviceTokenDto
{
    public int DeviceTokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
