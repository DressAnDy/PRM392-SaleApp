using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Infrastructure.Data;

namespace SaleApp.API.Controllers;

/// <summary>
/// Mobile-specific payment endpoints for Android/iOS apps.
/// </summary>
[ApiController]
[Route("api/mobile/payments")]
[Authorize]
public class MobilePaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly SaleAppDbContext _context;
    private readonly ILogger<MobilePaymentController> _logger;

    public MobilePaymentController(
        IVnPayService vnPayService,
        SaleAppDbContext context,
        ILogger<MobilePaymentController> logger)
    {
        _vnPayService = vnPayService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a VNPay payment URL for mobile app.
    /// The mobile app opens this URL in Custom Tabs / WebView.
    /// On completion VNPay redirects to saleapp://payment/callback (deep link).
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(VnPayCreateUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreateMobilePaymentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating mobile payment for order {OrderId}", request.OrderId);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var response = await _vnPayService.CreatePaymentUrlAsync(
                request.OrderId,
                ipAddress,
                isMobile: true,
                useSDK: true);

            _logger.LogInformation(
                "Mobile payment URL created successfully for order {OrderId}", request.OrderId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Failed to create mobile payment for order {OrderId}: {Message}",
                request.OrderId, ex.Message);

            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating mobile payment for order {OrderId}", request.OrderId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get payment status by payment ID.
    /// Mobile app calls this after VNPay redirects to the deep link to verify the result.
    /// Automatically expires payments that have been pending for over 15 minutes.
    /// </summary>
    [HttpGet("{paymentId}/status")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(int paymentId)
    {
        try
        {
            // AsTracking() ghi đè global NoTracking để EF phát hiện thay đổi khi auto-expire
            var payment = await _context.Payments
                .AsTracking()
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                return NotFound(new ErrorResponse { Error = "Payment not found" });

            // Auto-expire pending payments older than 15 minutes.
            if (payment.PaymentStatus == "Pending")
            {
                var minutesSinceCreation = (DateTime.UtcNow - payment.CreatedAt).TotalMinutes;

                if (minutesSinceCreation > 15)
                {
                    payment.PaymentStatus = "Expired";
                    payment.Order.OrderStatus = "Cancelled";
                    payment.Order.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Payment {PaymentId} auto-expired after {Minutes:F1} minutes",
                        paymentId, minutesSinceCreation);
                }
            }

            var response = new PaymentStatusResponse
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                Status = payment.PaymentStatus,
                IsPaid = payment.PaymentStatus == "Paid",
                TransactionId = payment.ProviderTransactionId,
                PaidAt = payment.PaidAt,
                Message = GetStatusMessage(payment.PaymentStatus),
                CanRetry = payment.PaymentStatus is "Failed" or "Expired"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting payment status for payment {PaymentId}", paymentId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Internal server error" });
        }
    }

    private static string GetStatusMessage(string status) => status switch
    {
        "Pending" => "Đang chờ thanh toán",
        "Paid" => "Thanh toán thành công",
        "Failed" => "Thanh toán thất bại",
        "Expired" => "Đã hết hạn thanh toán (quá 15 phút)",
        "Cancelled" => "Đã hủy",
        _ => "Trạng thái không xác định"
    };
}