using System.Net;
using System.Security.Cryptography;
using System.Text;
using Contracts.External.Payments;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Payments;

public class VNPAYPaymentService(IConfiguration configuration) : IPaymentService
{
    public PaymentProvider Provider => PaymentProvider.VNPAY;

    private readonly string _tmnCode = configuration["VNPAY:TmnCode"] ??
                                       throw new InvalidOperationException("VNPAY:TmnCode is not configured.");

    private readonly string _hashSecret = configuration["VNPAY:HashSecret"] ??
                                          throw new InvalidOperationException("VNPAY:HashSecret is not configured.");

    private readonly string _baseUrl = configuration["VNPAY:BaseUrl"] ??
                                       throw new InvalidOperationException("VNPAY:BaseUrl is not configured.");

    private readonly string _version = configuration["VNPAY:Version"] ??
                                       throw new InvalidOperationException("VNPAY:Version is not configured.");

    private readonly string _currCode = configuration["VNPAY:CurrCode"] ??
                                        throw new InvalidOperationException("VNPAY:CurrCode is not configured.");

    private readonly string _locale = configuration["VNPAY:Locale"] ??
                                      throw new InvalidOperationException("VNPAY:Locale is not configured.");

    public Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequestBase request)
    {
        if (request is not CreateVNPAYPaymentRequest vnpayRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(CreateVNPAYPaymentRequest)}.",
                nameof(request));
        }

        try
        {
            var txnRef = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var description = string.IsNullOrWhiteSpace(vnpayRequest.Description)
                ? $"Thanh toan don hang {txnRef}"
                : vnpayRequest.Description;
            if (description.Length > 255)
            {
                description = description[..255];
            }

            var data = string.Join('&', new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["vnp_Version"] = _version,
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = _tmnCode,
                    ["vnp_Amount"] = decimal.ToInt64(vnpayRequest.Amount * 100).ToString(),
                    ["vnp_CurrCode"] = _currCode,
                    ["vnp_TxnRef"] = txnRef,
                    ["vnp_OrderInfo"] = description,
                    ["vnp_OrderType"] = string.IsNullOrWhiteSpace(vnpayRequest.OrderType)
                        ? "other"
                        : vnpayRequest.OrderType,
                    ["vnp_Locale"] = _locale,
                    ["vnp_ReturnUrl"] = vnpayRequest.ReturnUrl,
                    ["vnp_IpAddr"] = vnpayRequest.IpAddress,
                    ["vnp_CreateDate"] =
                        DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss"),
                    ["vnp_ExpireDate"] = new DateTimeOffset(vnpayRequest.ExpiredAt.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(vnpayRequest.ExpiredAt, DateTimeKind.Utc)
                            : vnpayRequest.ExpiredAt.ToUniversalTime())
                        .ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss")
                }
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
            return Task.FromResult(new CreatePaymentResponse
            {
                PaymentReferenceId = txnRef,
                PaymentUrl = $"{_baseUrl}?{data}&vnp_SecureHash={HmacSha512(_hashSecret, data)}"
            });
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Tạo yêu cầu thanh toán VNPAY thất bại: {e.Message}", e);
        }
    }

    public Task<ProcessIpnResponse> ProcessIpnAsync(ProcessIpnRequestBase request)
    {
        if (request is not VNPAYIpnRequestDto vnpayRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(VNPAYIpnRequestDto)}.",
                nameof(request));
        }

        try
        {
            var isValidSignature = IsValidSignature(vnpayRequest);
            return Task.FromResult(new ProcessIpnResponse
            {
                PaymentReferenceId = vnpayRequest.vnp_TxnRef,
                IsValidSignature = isValidSignature,
                IsPaymentSuccess = isValidSignature && vnpayRequest is
                    { vnp_ResponseCode: "00", vnp_TransactionStatus: null or "00" }
            });
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Xử lý IPN VNPAY thất bại: {e.Message}", e);
        }
    }

    private bool IsValidSignature(VNPAYIpnRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.vnp_SecureHash))
        {
            return false;
        }

        var sortedParams =
            new SortedDictionary<string, string>(request.ToParameterDictionary(), StringComparer.Ordinal);

        var signDataEncoded = string.Join('&',
            sortedParams.Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
        if (request.vnp_SecureHash.Equals(HmacSha512(_hashSecret, signDataEncoded), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var signDataRaw = string.Join('&', sortedParams.Select(p => $"{p.Key}={p.Value}"));
        return request.vnp_SecureHash.Equals(HmacSha512(_hashSecret, signDataRaw), StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSha512(string key, string inputData) =>
        Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(inputData)));
}

public class VNPAYHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement a real health check for VNPAY in production.
            return Task.FromResult(HealthCheckResult.Healthy("Kết nối đến VNPAY thành công."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kết nối đến VNPAY thất bại.", ex));
        }
    }
}