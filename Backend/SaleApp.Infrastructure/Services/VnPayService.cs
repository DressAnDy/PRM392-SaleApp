using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class VnPayService : IVnPayService
{
    // ICT = UTC+7
    private static readonly TimeZoneInfo _ict = TimeZoneInfo.CreateCustomTimeZone(
        "ICT", TimeSpan.FromHours(7), "Indochina Time", "Indochina Time");

    private readonly SaleAppDbContext _context;
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _paymentUrl;
    private readonly string _returnUrl;
    private readonly string _version;
    private readonly string _command;

    public VnPayService(SaleAppDbContext context, IConfiguration configuration)
    {
        _context = context;

        var vnpay = configuration.GetSection("VNPay");
        _tmnCode = vnpay["TmnCode"] ?? throw new InvalidOperationException("VNPay:TmnCode not configured");
        _hashSecret = vnpay["HashSecret"] ?? throw new InvalidOperationException("VNPay:HashSecret not configured");
        _paymentUrl = vnpay["PaymentUrl"] ?? throw new InvalidOperationException("VNPay:PaymentUrl not configured");
        _returnUrl = vnpay["ReturnUrl"] ?? throw new InvalidOperationException("VNPay:ReturnUrl not configured");
        _version = vnpay["Version"] ?? "2.1.0";
        _command = vnpay["Command"] ?? "pay";
    }

    // ─────────────────────────── Public Methods ────────────────────────────

    public async Task<VnPayCreateUrlResponse> CreatePaymentUrlAsync(int orderId, string ipAddress)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PaymentStatus == "Pending")
            ?? throw new InvalidOperationException($"No pending payment found for order {orderId}.");

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _ict);
        var expireDate = now.AddMinutes(15);

        // VNPay amount is in "smallest currency unit" — VND has no sub-unit so multiply by 100
        var amount = (long)(payment.Amount * 100);

        var requestData = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _version,
            ["vnp_Command"] = _command,
            ["vnp_TmnCode"] = _tmnCode,
            ["vnp_Amount"] = amount.ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = payment.PaymentId.ToString(),
            ["vnp_OrderInfo"] = $"Thanh toan don hang #{orderId}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _returnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
        };

        var queryString = BuildQueryString(requestData);
        var secureHash = ComputeHmacSha512(_hashSecret, queryString);

        var paymentUrl = $"{_paymentUrl}?{queryString}&vnp_SecureHash={secureHash}";

        return new VnPayCreateUrlResponse { PaymentUrl = paymentUrl };
    }

    public async Task<VnPayCallbackResponse> HandleCallbackAsync(IDictionary<string, string> queryParams)
    {
        // 0. Kiểm tra input null hoặc rỗng
        if (queryParams == null || !queryParams.Any())
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = false,
                Message = "No query parameters received."
            };
        }

        // 1. Lấy vnp_SecureHash từ query
        queryParams.TryGetValue("vnp_SecureHash", out var receivedHash);
        receivedHash ??= string.Empty;

        // 2. Tạo SortedDictionary chỉ chứa các tham số vnp_ (trừ SecureHash)
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kvp in queryParams)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (key.StartsWith("vnp_") &&
                key != "vnp_SecureHash" &&
                key != "vnp_SecureHashType")
            {
                vnpParams[key] = value ?? string.Empty;
            }
        }

        // 3. Tạo query string và tính hash để verify
        var queryString = BuildQueryString(vnpParams);
        var computedHash = ComputeHmacSha512(_hashSecret, queryString);

        if (!string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase))
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = false,
                Message = "Invalid signature."
            };
        }

        // 4. Parse các trường quan trọng
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParams.TryGetValue("vnp_TransactionNo", out var transactionId);
        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);

        responseCode ??= string.Empty;
        transactionId ??= string.Empty;
        txnRef ??= string.Empty;

        if (!int.TryParse(txnRef, out var paymentId))
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = false,
                Message = "Invalid TxnRef (payment ID)."
            };
        }

        // 5. Load payment record từ DB
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = false,
                Message = "Payment record not found."
            };
        }

        // Guard: tránh xử lý lại nếu đã xử lý trước đó
        if (payment.PaymentStatus != "Pending")
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = responseCode == "00",
                Message = "Payment already processed.",
                OrderId = payment.OrderId,
                TransactionId = payment.ProviderTransactionId
            };
        }

        // 6. Cập nhật trạng thái dựa trên responseCode của VnPay
        if (responseCode == "00")
        {
            payment.PaymentStatus = "Paid";
            payment.PaidAt = DateTime.UtcNow;
            payment.ProviderTransactionId = transactionId;
            payment.Order.OrderStatus = "Confirmed";
        }
        else
        {
            payment.PaymentStatus = "Failed";
            payment.ProviderTransactionId = transactionId;
            payment.Order.OrderStatus = "Cancelled";
        }

        payment.Order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 7. Return kết quả
        return new VnPayCallbackResponse
        {
            IsSuccess = responseCode == "00",
            Message = responseCode == "00"
                ? "Payment successful."
                : $"Payment failed (code: {responseCode}).",
            OrderId = payment.OrderId,
            TransactionId = transactionId
        };
    }
    // ─────────────────────────── Private Helpers ───────────────────────────

    /// <summary>
    /// Builds URL-encoded query string from a sorted dictionary.
    /// Values are percent-encoded (RFC 3986 — spaces become %20, not +).
    /// </summary>
    private static string BuildQueryString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Computes HMAC-SHA512 of <paramref name="data"/> using <paramref name="key"/>.
    /// Returns lowercase hex string.
    /// </summary>
    private static string ComputeHmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
