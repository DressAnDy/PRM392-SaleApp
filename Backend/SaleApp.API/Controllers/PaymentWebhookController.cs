using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

/// <summary>
/// Server-side VNPay webhook endpoints.
/// These are NOT called by the mobile app directly.
/// </summary>
[ApiController]
[Route("api/payments/vnpay")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IVnPayService vnPayService,
        ILogger<PaymentWebhookController> logger)
    {
        _vnPayService = vnPayService;
        _logger = logger;
    }

    /// <summary>
    /// IPN (Instant Payment Notification) endpoint.
    /// VNPay calls this server-to-server when payment is completed.
    /// Configure this URL in the VNPay merchant portal.
    /// </summary>
    [HttpGet("ipn")]
    [HttpPost("ipn")]
    public async Task<IActionResult> VnPayIPN()
    {
        try
        {
            var rawQuery = Request.QueryString.Value ?? string.Empty;

            _logger.LogInformation(
                "VNPay IPN received — raw query length: {Length}", rawQuery.Length);

            var result = await _vnPayService.HandleCallbackAsync(rawQuery);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "VNPay IPN processed successfully — OrderId: {OrderId}, TransactionId: {TransactionId}",
                    result.OrderId, result.TransactionId);

                // VNPay requires this exact response format to acknowledge receipt.
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }

            _logger.LogWarning(
                "VNPay IPN validation failed: {Message}", result.Message);

            return Ok(new { RspCode = "97", Message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay IPN");
            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }
    }

    /// <summary>
    /// Return URL endpoint — the user's browser is redirected here from VNPay after payment.
    /// For web: serves as the landing page after payment.
    /// For mobile: not used (mobile receives the deep link saleapp://payment/callback instead).
    /// </summary>
    [HttpGet("return")]
    public async Task<IActionResult> VnPayReturn()
    {
        try
        {
            var rawQuery = Request.QueryString.Value ?? string.Empty;
            var result = await _vnPayService.HandleCallbackAsync(rawQuery);

            _logger.LogInformation(
                "VNPay Return callback — OrderId: {OrderId}, Success: {Success}",
                result.OrderId, result.IsSuccess);

            var redirectUrl = result.IsSuccess
                ? $"/payment/success?orderId={result.OrderId}"
                : $"/payment/failed?orderId={result.OrderId}&message={Uri.EscapeDataString(result.Message)}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return callback");
            return Redirect("/payment/error");
        }
    }
}
