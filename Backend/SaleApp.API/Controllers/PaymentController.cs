using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;

    public PaymentController(IVnPayService vnPayService)
    {
        _vnPayService = vnPayService;
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        return int.Parse(value);
    }

    /// <summary>
    /// Creates a VNPay payment URL for the pending payment of the given order.
    /// The order must have a Payment record with Status=Pending.
    /// </summary>
    [HttpPost("vnpay/create-url")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] VnPayCreateUrlRequest request)
    {
        // Capture the real client IP (handle reverse-proxy scenarios)
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";

        try
        {
            var result = await _vnPayService.CreatePaymentUrlAsync(request.OrderId, ipAddress);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// VNPay return URL handler. VNPay redirects the user's browser here after payment.
    /// The frontend (ReturnUrl) forwards the full query string to this endpoint.
    /// </summary>
    [HttpGet("vnpay/callback")]
    public async Task<IActionResult> VnPayCallback()
    {
        try
        {
            var queryDict = Request.Query
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            var result = await _vnPayService.HandleCallbackAsync(queryDict);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
