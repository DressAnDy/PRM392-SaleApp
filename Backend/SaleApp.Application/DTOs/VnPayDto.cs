namespace SaleApp.Application.DTOs;

public class VnPayCreateUrlRequest
{
    public int OrderId { get; set; }
}

public class VnPayCreateUrlResponse
{
    public string PaymentUrl { get; set; } = string.Empty;
}

public class VnPayCallbackResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? TransactionId { get; set; }
}
