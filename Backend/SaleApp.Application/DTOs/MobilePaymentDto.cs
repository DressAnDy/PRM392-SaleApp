namespace SaleApp.Application.DTOs;

public class CreateMobilePaymentRequest
{
    public int OrderId { get; set; }
}

public class PaymentStatusResponse
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool CanRetry { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
