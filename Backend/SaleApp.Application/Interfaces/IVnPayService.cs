using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IVnPayService
{
    /// <summary>
    /// Builds and returns the VNPay payment URL for the pending Payment linked to the order.
    /// </summary>
    Task<VnPayCreateUrlResponse> CreatePaymentUrlAsync(int orderId, string ipAddress);

    /// <summary>
    /// Validates the VNPay callback signature and updates the Payment/Order status accordingly.
    /// </summary>
    Task<VnPayCallbackResponse> HandleCallbackAsync(IDictionary<string, string> queryParams);
}
