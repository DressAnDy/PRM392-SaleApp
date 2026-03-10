using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IVnPayService
{
    /// <summary>
    /// Builds and returns the VNPay payment URL for the pending Payment linked to the order.
    /// <para><paramref name="isMobile"/>: true → Custom Tabs deep link; <paramref name="useSDK"/>: true → VNPay SDK URL.</para>
    /// </summary>
    Task<VnPayCreateUrlResponse> CreatePaymentUrlAsync(int orderId, string ipAddress, bool isMobile = false, bool useSDK = false);

    /// <summary>
    /// Validates the VNPay callback signature and updates the Payment/Order status accordingly.
    /// Pass <paramref name="rawQueryString"/> exactly as received from VNPay (e.g. Request.QueryString.Value)
    /// so the signature is verified over the original URL-encoded bytes without any decode/re-encode round-trip.
    /// </summary>
    Task<VnPayCallbackResponse> HandleCallbackAsync(string rawQueryString);
}
