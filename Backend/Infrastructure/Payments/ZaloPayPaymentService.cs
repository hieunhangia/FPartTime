using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contracts.External.Payments;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Payments;

public class ZaloPayPaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IPaymentService
{
    public const string HttpClientName = "ZaloPayClient";

    public PaymentProvider Provider => PaymentProvider.ZaloPay;

    private readonly int _appId = int.TryParse(configuration["ZaloPay:AppId"], out var id)
        ? id
        : throw new InvalidOperationException("ZaloPay:AppId chưa được cấu hình hoặc không hợp lệ.");

    private readonly string _key1 = configuration["ZaloPay:Key1"] ??
                                    throw new InvalidOperationException("ZaloPay:Key1 chưa được cấu hình.");

    private readonly string _key2 = configuration["ZaloPay:Key2"] ??
                                    throw new InvalidOperationException("ZaloPay:Key2 chưa được cấu hình.");

    private readonly string _createOrderUrl = configuration["ZaloPay:CreateOrderUrl"] ??
                                              throw new InvalidOperationException(
                                                  "ZaloPay:CreateOrderUrl chưa được cấu hình.");

    private readonly string _callbackUrl = configuration["ZaloPay:CallbackUrl"] ??
                                           throw new InvalidOperationException(
                                               "ZaloPay:CallbackUrl chưa được cấu hình.");

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequestBase request)
    {
        if (request is not CreateZaloPayPaymentRequest zaloPayRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(CreateZaloPayPaymentRequest)}.",
                nameof(request));
        }

        try
        {
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appTransId = $"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):yyMMdd}_{appTime}";
            var amount = decimal.ToInt64(zaloPayRequest.Amount);

            var description = string.IsNullOrWhiteSpace(zaloPayRequest.Description)
                ? $"Thanh toan don hang #{appTransId}"
                : zaloPayRequest.Description;
            if (description.Length > 256)
            {
                description = description[..256];
            }

            var appUser = string.IsNullOrWhiteSpace(zaloPayRequest.AppUser) ? "User" : zaloPayRequest.AppUser;
            if (appUser.Length > 50)
            {
                appUser = appUser[..50];
            }

            var embedDataObj = new Dictionary<string, string>
            {
                ["redirecturl"] = zaloPayRequest.ReturnUrl
            };
            var embedDataJson = JsonSerializer.Serialize(embedDataObj);

            var itemJson = JsonSerializer.Serialize(zaloPayRequest.Items.Select(i => new
            {
                itemname = i.Name,
                itemprice = decimal.ToInt64(i.Price),
                itemquantity = i.Quantity
            }));
            if (itemJson.Length > 2048)
            {
                itemJson = "[]";
            }

            var postData = new Dictionary<string, string>
            {
                ["app_id"] = _appId.ToString(),
                ["app_user"] = appUser,
                ["app_trans_id"] = appTransId,
                ["app_time"] = appTime.ToString(),
                ["expire_duration_seconds"] =
                    ((long)(zaloPayRequest.ExpiredAt - DateTime.UtcNow).TotalSeconds).ToString(),
                ["amount"] = amount.ToString(),
                ["item"] = itemJson,
                ["description"] = description,
                ["embed_data"] = embedDataJson,
                ["bank_code"] = string.Empty,
                ["mac"] = HmacSha256Hex(_key1,
                    $"{_appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedDataJson}|{itemJson}"),
                ["callback_url"] = _callbackUrl
            };

            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var response = await httpClient.PostAsync(_createOrderUrl, new FormUrlEncodedContent(postData));
            var responseJson = await response.Content.ReadFromJsonAsync<ZaloPayCreateOrderResponse>()
                               ?? throw new InternalServerErrorException(
                                   "Không nhận được phản hồi từ ZaloPay hoặc dữ liệu không hợp lệ.");
            if (responseJson.ReturnCode != 1 || string.IsNullOrWhiteSpace(responseJson.OrderUrl))
            {
                throw new InternalServerErrorException(
                    $"Tạo yêu cầu thanh toán ZaloPay thất bại: {responseJson.ReturnMessage} (mã trạng thái chi tiết: {responseJson.SubReturnCode?.ToString() ?? "N/A"} - {responseJson.SubReturnMessage ?? "N/A"}).");
            }

            return new CreatePaymentResponse
            {
                PaymentReferenceId = appTransId,
                PaymentUrl = responseJson.OrderUrl
            };
        }
        catch (InternalServerErrorException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Tạo yêu cầu thanh toán ZaloPay thất bại: {e.Message}", e);
        }
    }

    public Task<ProcessIpnResponse> ProcessIpnAsync(ProcessIpnRequestBase request)
    {
        if (request is not ZaloPayCallbackRequestDto callbackRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(ZaloPayCallbackRequestDto)}.",
                nameof(request));
        }

        try
        {
            var expectedMac = HmacSha256Hex(_key2, callbackRequest.Data);
            var isValidSignature = string.Equals(expectedMac, callbackRequest.Mac, StringComparison.OrdinalIgnoreCase);

            if (!isValidSignature)
            {
                return Task.FromResult(new ProcessIpnResponse
                {
                    IsValidSignature = false,
                    IsPaymentSuccess = false
                });
            }

            var callbackData = JsonSerializer.Deserialize<ZaloPayCallbackRequestDto.ZaloPayCallbackDataDto>(
                callbackRequest.Data, JsonSerializerOptions.Web);

            return Task.FromResult(new ProcessIpnResponse
            {
                PaymentReferenceId = callbackData?.AppTransId,
                IsValidSignature = true,
                IsPaymentSuccess = callbackData != null && !string.IsNullOrEmpty(callbackData.AppTransId)
            });
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Xử lý Callback ZaloPay thất bại: {e.Message}", e);
        }
    }

    private static string HmacSha256Hex(string key, string data) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data)));

    private class ZaloPayCreateOrderResponse
    {
        [JsonPropertyName("return_code")]
        [Required]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        [Required]
        public string ReturnMessage { get; set; } = string.Empty;

        [JsonPropertyName("sub_return_code")] public int? SubReturnCode { get; set; }

        [JsonPropertyName("sub_return_message")]
        public string? SubReturnMessage { get; set; }

        [JsonPropertyName("order_url")] public string? OrderUrl { get; set; }
        [JsonPropertyName("zp_trans_token")] public string? ZpTransToken { get; set; }
        [JsonPropertyName("order_token")] public string? OrderToken { get; set; }
        [JsonPropertyName("qr_code")] public string? QrCode { get; set; }
    }
}

public class ZaloPayHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(ZaloPayPaymentService.HttpClientName);
            using var response = await client.GetAsync(configuration["ZaloPay:HealthCheckUrl"] ??
                                                       throw new InvalidOperationException(
                                                           "ZaloPay:HealthCheckUrl chưa được cấu hình."),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            return HealthCheckResult.Healthy("Kết nối đến ZaloPay thành công.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Kết nối đến ZaloPay thất bại.", ex);
        }
    }
}