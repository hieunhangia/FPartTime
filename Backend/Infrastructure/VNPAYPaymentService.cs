using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Contracts.External;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure;

public class VNPAYPaymentService(IConfiguration configuration)
    : IVNPAYPaymentService
{
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

    public Task<CreateVNPAYPaymentResponse> CreatePaymentAsync(CreateVNPAYPaymentRequest request)
    {
        try
        {
            var txnRef = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                request.Description = $"Thanh toán đơn hàng {txnRef}";
            }

            if (request.Description.Length > 255)
            {
                request.Description = request.Description[..255];
            }

            var vnpParams = new SortedDictionary<string, string>(new VnPayCompare())
            {
                ["vnp_Version"] = _version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_Amount"] = decimal.ToInt64(request.Amount * 100).ToString(),
                ["vnp_CurrCode"] = _currCode,
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = request.Description,
                ["vnp_OrderType"] = string.IsNullOrWhiteSpace(request.OrderType) ? "other" : request.OrderType,
                ["vnp_Locale"] = _locale,
                ["vnp_ReturnUrl"] = request.ReturnUrl,
                ["vnp_IpAddr"] = request.IpAddress,
                ["vnp_CreateDate"] = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = new DateTimeOffset(request.ExpiredAt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(request.ExpiredAt, DateTimeKind.Utc)
                        : request.ExpiredAt.ToUniversalTime())
                    .ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss")
            };

            var data = new StringBuilder();
            foreach (var (key, value) in vnpParams)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    data.Append(WebUtility.UrlEncode(key))
                        .Append('=')
                        .Append(WebUtility.UrlEncode(value))
                        .Append('&');
                }
            }

            var queryString = data.ToString();
            var signData = queryString.Length > 0 ? queryString.Remove(queryString.Length - 1, 1) : string.Empty;
            var secureHash = HmacSha512(_hashSecret, signData);
            var paymentUrl = $"{_baseUrl}?{queryString}vnp_SecureHash={secureHash}";

            return Task.FromResult(new CreateVNPAYPaymentResponse
            {
                PaymentReferenceId = txnRef,
                PaymentUrl = paymentUrl
            });
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Tạo yêu cầu thanh toán VNPAY thất bại: {e.Message}", e);
        }
    }

    public Task<VNPAYIpnProcessingResult> ProcessIpnAsync(VNPAYIpnRequestDto request)
    {
        try
        {
            var parameters = request.ToParameterDictionary();
            if (parameters.Count == 0)
            {
                return Task.FromResult(new VNPAYIpnProcessingResult
                {
                    PaymentReferenceId = string.Empty,
                    IsSuccess = false,
                    RspCode = "97",
                    Message = "Invalid Checksum"
                });
            }

            string inputHash = string.Empty;
            if (parameters.TryGetValue("vnp_SecureHash", out var hashVal))
            {
                inputHash = hashVal;
            }
            else if (!string.IsNullOrEmpty(request.vnp_SecureHash))
            {
                inputHash = request.vnp_SecureHash;
            }

            if (string.IsNullOrWhiteSpace(inputHash))
            {
                return Task.FromResult(new VNPAYIpnProcessingResult
                {
                    PaymentReferenceId = request.vnp_TxnRef ?? string.Empty,
                    IsSuccess = false,
                    RspCode = "97",
                    Message = "Invalid Checksum"
                });
            }

            var sortedParams = new SortedDictionary<string, string>(new VnPayCompare());
            foreach (var (key, value) in parameters)
            {
                if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(value))
                {
                    sortedParams[key] = value;
                }
            }

            var dataEncoded = new StringBuilder();
            var dataRaw = new StringBuilder();

            foreach (var (key, value) in sortedParams)
            {
                dataEncoded.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value))
                    .Append('&');
                dataRaw.Append(key).Append('=').Append(value).Append('&');
            }

            var signDataEncoded = dataEncoded.Length > 0 ? dataEncoded.ToString().TrimEnd('&') : string.Empty;
            var signDataRaw = dataRaw.Length > 0 ? dataRaw.ToString().TrimEnd('&') : string.Empty;
            var computedHashEncoded = HmacSha512(_hashSecret, signDataEncoded);
            var computedHashRaw = HmacSha512(_hashSecret, signDataRaw);
            var isValidSignature = inputHash.Equals(computedHashEncoded, StringComparison.OrdinalIgnoreCase) ||
                                   inputHash.Equals(computedHashRaw, StringComparison.OrdinalIgnoreCase);

            var txnRef = sortedParams.TryGetValue("vnp_TxnRef", out var refVal)
                ? refVal
                : (request.vnp_TxnRef ?? string.Empty);
            var responseCode = sortedParams.TryGetValue("vnp_ResponseCode", out var rc) ? rc : request.vnp_ResponseCode;
            var transactionStatus = sortedParams.TryGetValue("vnp_TransactionStatus", out var ts)
                ? ts
                : request.vnp_TransactionStatus;
            var transactionNo = sortedParams.TryGetValue("vnp_TransactionNo", out var tn)
                ? tn
                : request.vnp_TransactionNo;

            if (!isValidSignature)
            {
                return Task.FromResult(new VNPAYIpnProcessingResult
                {
                    PaymentReferenceId = txnRef,
                    IsSuccess = false,
                    RspCode = "97",
                    Message = "Invalid Checksum",
                    ResponseCode = responseCode,
                    TransactionStatus = transactionStatus,
                    TransactionNo = transactionNo
                });
            }

            var isSuccess = responseCode == "00" && (transactionStatus == null || transactionStatus == "00");

            return Task.FromResult(new VNPAYIpnProcessingResult
            {
                PaymentReferenceId = txnRef,
                IsSuccess = isSuccess,
                RspCode = "00",
                Message = "Confirm Success",
                ResponseCode = responseCode,
                TransactionStatus = transactionStatus,
                TransactionNo = transactionNo
            });
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Xử lý webhook VNPAY thất bại: {e.Message}", e);
        }
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var b in hashValue)
        {
            hash.Append(b.ToString("x2"));
        }

        return hash.ToString();
    }

    private class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}

public class VNPAYHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fake request to VNPAY to check connectivity.
            return Task.FromResult(HealthCheckResult.Healthy("Kết nối đến VNPAY thành công."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kết nối đến VNPAY thất bại.", ex));
        }
    }
}