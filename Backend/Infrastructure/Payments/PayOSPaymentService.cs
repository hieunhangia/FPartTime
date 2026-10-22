using Contracts.External.Payments;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using InternalServerErrorException = Domain.Shared.Exceptions.HttpErrorExceptions.InternalServerErrorException;

namespace Infrastructure.Payments;

public class PayOSPaymentService(IConfiguration configuration) : IPaymentService
{
    public PaymentProvider Provider => PaymentProvider.PayOS;

    private readonly PayOSClient _payOSClient = new(new PayOSOptions
    {
        ClientId = configuration["PayOS:ClientId"],
        ApiKey = configuration["PayOS:ApiKey"],
        ChecksumKey = configuration["PayOS:ChecksumKey"]
    });

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequestBase request)
    {
        if (request is not CreatePayOSPaymentRequest payOsRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(CreatePayOSPaymentRequest)}.",
                nameof(request));
        }

        try
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var description = string.IsNullOrWhiteSpace(payOsRequest.Description)
                ? orderCode.ToString()
                : payOsRequest.Description;

            if (description.Length > 15)
            {
                description = description[..15];
            }

            var result = await _payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = decimal.ToInt64(payOsRequest.Amount),
                Description = description,
                CancelUrl = payOsRequest.CancelUrl,
                ReturnUrl = payOsRequest.ReturnUrl,
                ExpiredAt = new DateTimeOffset(payOsRequest.ExpiredAt.ToUniversalTime()).ToUnixTimeSeconds(),
                Items =
                [
                    .. payOsRequest.Items.Select(i => new PaymentLinkItem
                    {
                        Name = i.Name,
                        Price = decimal.ToInt64(i.Price),
                        Quantity = i.Quantity
                    })
                ]
            });

            return new CreatePaymentResponse
            {
                PaymentReferenceId = result.OrderCode.ToString(),
                PaymentUrl = result.CheckoutUrl
            };
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Tạo yêu cầu thanh toán PayOS thất bại: {e.Message}", e);
        }
    }

    public async Task<ProcessIpnResponse> ProcessIpnAsync(ProcessIpnRequestBase request)
    {
        if (request is not PayOSWebhookRequestDto payOsRequest)
        {
            throw new ArgumentException(
                $"Yêu cầu không hợp lệ cho cổng thanh toán {Provider}. Yêu cầu {nameof(PayOSWebhookRequestDto)}.",
                nameof(request));
        }

        try
        {
            var webhookData = await _payOSClient.Webhooks.VerifyAsync(new Webhook
            {
                Code = payOsRequest.Code,
                Description = payOsRequest.Desc,
                Success = payOsRequest.Success,
                Signature = payOsRequest.Signature,
                Data = new WebhookData
                {
                    OrderCode = payOsRequest.Data.OrderCode,
                    Amount = payOsRequest.Data.Amount,
                    Description = payOsRequest.Data.Description,
                    AccountNumber = payOsRequest.Data.AccountNumber,
                    Reference = payOsRequest.Data.Reference,
                    TransactionDateTime = payOsRequest.Data.TransactionDateTime,
                    Currency = payOsRequest.Data.Currency,
                    PaymentLinkId = payOsRequest.Data.PaymentLinkId,
                    Code = payOsRequest.Data.Code,
                    Description2 = payOsRequest.Data.Desc,
                    CounterAccountBankId = payOsRequest.Data.CounterAccountBankId,
                    CounterAccountBankName = payOsRequest.Data.CounterAccountBankName,
                    CounterAccountName = payOsRequest.Data.CounterAccountName,
                    CounterAccountNumber = payOsRequest.Data.CounterAccountNumber,
                    VirtualAccountName = payOsRequest.Data.VirtualAccountName,
                    VirtualAccountNumber = payOsRequest.Data.VirtualAccountNumber
                }
            });
            return new ProcessIpnResponse
            {
                PaymentReferenceId = webhookData.OrderCode.ToString(),
                IsValidSignature = true,
                IsPaymentSuccess = true
            };
        }
        catch (WebhookException)
        {
            return new ProcessIpnResponse
            {
                IsValidSignature = false,
                IsPaymentSuccess = false
            };
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Xử lý webhook PayOS thất bại: {e.Message}", e);
        }
    }
}

public class PayOSHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HealthCheckHttpClientName = "PayOSHealthCheck";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HealthCheckHttpClientName);
            using var response = await client.GetAsync("/", cancellationToken);
            response.EnsureSuccessStatusCode();
            return HealthCheckResult.Healthy("Kết nối đến PayOS thành công.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Kết nối đến PayOS thất bại.", ex);
        }
    }
}