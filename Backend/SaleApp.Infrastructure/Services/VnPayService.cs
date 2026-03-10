using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<VnPayService> _logger;
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _paymentUrl;
    private readonly string _version;
    private readonly string _command;

    public VnPayService(SaleAppDbContext context, IConfiguration configuration, ILogger<VnPayService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        var vnpay = configuration.GetSection("VNPay");
        _tmnCode = vnpay["TmnCode"] ?? throw new InvalidOperationException("VNPay:TmnCode not configured");
        _hashSecret = vnpay["HashSecret"] ?? throw new InvalidOperationException("VNPay:HashSecret not configured");
        _paymentUrl = vnpay["PaymentUrl"] ?? throw new InvalidOperationException("VNPay:PaymentUrl not configured");
        _version = vnpay["Version"] ?? "2.1.0";
        _command = vnpay["Command"] ?? "pay";
    }

    // ─────────────────────────── Public Methods ────────────────────────────

    public async Task<VnPayCreateUrlResponse> CreatePaymentUrlAsync(
        int orderId,
        string ipAddress,
        bool isMobile = false,
        bool useSDK = false)
    {
        _logger.LogInformation(
            "Creating payment URL — OrderId: {OrderId}, IpAddress: {IpAddress}, IsMobile: {IsMobile}, UseSDK: {UseSDK}",
            orderId, ipAddress, isMobile, useSDK);

        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PaymentStatus == "Pending")
            ?? throw new InvalidOperationException($"No pending payment found for order {orderId}.");

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _ict);
        var expireDate = now.AddMinutes(15);

        // VNPay requires amount in VND * 100 per their spec.
        // Prices in DB are stored in units of 1,000 VND (nghìn đồng),
        // so conversion: amount(nghìn) × 1,000 (→ VND) × 100 (→ VNPay unit) = × 100,000
        var amount = (long)(payment.Amount * 100_000);

        // Append timestamp to vnp_TxnRef so every URL generation is unique.
        // VNPay rejects duplicate TxnRef that already exist in their system.
        var txnRef = $"{payment.PaymentId}{now:yyyyMMddHHmmss}";

        string returnUrl;
        if (useSDK)
            returnUrl = _configuration["VNPay:ReturnUrl:MobileSDK"];
        else if (isMobile)
            returnUrl = _configuration["VNPay:ReturnUrl:MobileCustomTabs"] ?? "saleapp://payment/callback";
        else
            returnUrl = _configuration["VNPay:ReturnUrl:Web"];

        if (string.IsNullOrEmpty(returnUrl))
            throw new InvalidOperationException("VNPay ReturnUrl not configured");

        _logger.LogInformation(
            "ReturnUrl selected — IsMobile: {IsMobile}, UseSDK: {UseSDK}, URL: {ReturnUrl}",
            isMobile, useSDK, returnUrl);

        var requestData = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _version,
            ["vnp_Command"] = _command,
            ["vnp_TmnCode"] = _tmnCode,
            ["vnp_Amount"] = amount.ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = $"Thanh toan don hang #{orderId}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
        };

        // Hash input = URL-encoded query string (WebUtility, same as VNPay server).
        // URL = same string + &vnp_SecureHash=<hash>  (hash value itself is NOT encoded).
        var encodedData = BuildVnPayData(requestData);      // "key=val&key=val"
        var secureHash = ComputeHmacSha512(_hashSecret, encodedData);
        var paymentUrl = $"{_paymentUrl}?{encodedData}&vnp_SecureHash={secureHash}";

        _logger.LogInformation(
            "Payment URL created — PaymentId: {PaymentId}, UrlLength: {Length}",
            payment.PaymentId, paymentUrl.Length);

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

        // 2. Tạo SortedDictionary chỉ chứa các tham số vnp_ (trừ SecureHash + empty)
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kvp in queryParams)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (key.StartsWith("vnp_") &&
                key != "vnp_SecureHash" &&
                key != "vnp_SecureHashType" &&
                !string.IsNullOrEmpty(value))   // loại bỏ empty values
            {
                vnpParams[key] = value;
            }
        }

        // 3. Verify signature: build the same WebUtility-encoded string VNPay signed
        var encodedData = BuildVnPayData(vnpParams);
        var computedHash = ComputeHmacSha512(_hashSecret, encodedData);

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
        queryParams.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
        queryParams.TryGetValue("vnp_TransactionNo", out var transactionId);
        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);

        responseCode ??= string.Empty;
        transactionStatus ??= string.Empty;
        transactionId ??= string.Empty;
        txnRef ??= string.Empty;

        // vnp_TxnRef format: "{paymentId}{yyyyMMddHHmmss}" — timestamp is always 14 chars.
        var paymentIdStr = txnRef.Length > 14 ? txnRef[..^14] : txnRef;
        if (!int.TryParse(paymentIdStr, out var paymentId))
        {
            return new VnPayCallbackResponse
            {
                IsSuccess = false,
                Message = "Invalid TxnRef (payment ID)."
            };
        }

        // 5. Load payment record từ DB
        // AsTracking() ghi đè global NoTracking để EF phát hiện thay đổi và SaveChanges hoạt động
        var payment = await _context.Payments
            .AsTracking()
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
                IsSuccess = payment.PaymentStatus == "Paid",
                Message = "Payment already processed.",
                OrderId = payment.OrderId,
                TransactionId = payment.ProviderTransactionId,
                PaymentId = payment.PaymentId,
                PaidAt = payment.PaidAt
            };
        }

        // 6. Cập nhật trạng thái dựa trên responseCode + transactionStatus của VNPay
        // Cả hai phải là "00" mới coi là thành công
        var isSuccess = responseCode == "00" && transactionStatus == "00";

        if (isSuccess)
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
            IsSuccess = isSuccess,
            Message = isSuccess
                ? "Payment successful."
                : $"Payment failed (code: {responseCode}, transactionStatus: {transactionStatus}).",
            OrderId = payment.OrderId,
            TransactionId = transactionId,
            PaymentId = payment.PaymentId,
            PaidAt = isSuccess ? payment.PaidAt : null
        };
    }
    // ─────────────────────────── Private Helpers ───────────────────────────

    /// <summary>
    /// Builds a URL-encoded query string from a sorted dictionary using
    /// <see cref="WebUtility.UrlEncode"/> (spaces → +), exactly as VNPay does on their
    /// server. This same string is used as BOTH the HMAC-SHA512 input AND the URL
    /// query-string (vnp_SecureHash is appended separately, un-encoded).
    /// Empty values are excluded.
    /// </summary>
    private static string BuildVnPayData(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(WebUtility.UrlEncode(key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(value));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Computes HMAC-SHA512 and returns the result as a lowercase hex string.
    /// </summary>
    private static string ComputeHmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public Task<VnPayCallbackResponse> HandleCallbackAsync(string rawQueryString)
    {
        // Parse "?key=val&key=val" → dictionary, then delegate to the dict-based handler
        var queryParams = new Dictionary<string, string>(StringComparer.Ordinal);
        var qs = rawQueryString.StartsWith('?') ? rawQueryString[1..] : rawQueryString;

        foreach (var segment in qs.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = segment.IndexOf('=');
            if (eqIdx < 0) continue;
            var key = WebUtility.UrlDecode(segment[..eqIdx]);
            var value = WebUtility.UrlDecode(segment[(eqIdx + 1)..]);
            if (!string.IsNullOrEmpty(key))
                queryParams[key] = value;
        }

        return HandleCallbackAsync(queryParams);
    }
}