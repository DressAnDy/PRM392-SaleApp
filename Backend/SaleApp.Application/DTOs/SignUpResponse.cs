namespace SaleApp.Application.DTOs;

public class SignUpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
}
