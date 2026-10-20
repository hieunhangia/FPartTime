using Contracts.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Riok.Mapperly.Abstractions;
using BadRequestException = Domain.Shared.Exceptions.HttpErrorExceptions.BadRequestException;
using InternalServerErrorException = Domain.Shared.Exceptions.HttpErrorExceptions.InternalServerErrorException;

namespace Infrastructure;

public class PayOSPaymentService(IConfiguration configuration) : IPayOSPaymentService
{
    private readonly PayOSClient _payOSClient = new(new PayOSOptions
    {
        ClientId = configuration["PayOS:ClientId"],
        ApiKey = configuration["PayOS:ApiKey"],
        ChecksumKey = configuration["PayOS:ChecksumKey"]
    });

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        try
        {
            var result = await _payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
            {
                OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Amount = decimal.ToInt64(request.Amount),
                Description = request.Description,
                CancelUrl = request.CancelUrl,
                ReturnUrl = request.ReturnUrl,
                ExpiredAt = new DateTimeOffset(request.ExpiredAt.ToUniversalTime()).ToUnixTimeSeconds(),
                Items =
                [
                    .. request.Items.Select(i => new PaymentLinkItem
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

    public async Task CancelPaymentAsync(string paymentReferenceId)
    {
        try
        {
            await _payOSClient.PaymentRequests.CancelAsync(paymentReferenceId);
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Hủy yêu cầu thanh toán PayOS thất bại: {e.Message}", e);
        }
    }

    public async Task<WebhookProcessingResult> ProcessWebhookAsync(WebhookRequestDto request)
    {
        try
        {
            var webhookData = await _payOSClient.Webhooks.VerifyAsync(request.MapToSdkRequest());
            return new WebhookProcessingResult
            {
                PaymentReferenceId = webhookData.OrderCode.ToString()
            };
        }
        catch (WebhookException we)
        {
            throw new BadRequestException($"Xác thực webhook PayOS thất bại: {we.Message}", we);
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

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PayOSPaymentServiceMapper
{
    [MapProperty(nameof(WebhookRequestDto.Desc), nameof(Webhook.Description))]
    [MapProperty(nameof(WebhookRequestDto.Data.Desc), nameof(Webhook.Data.Description2))]
    public static partial Webhook MapToSdkRequest(this WebhookRequestDto source);
}