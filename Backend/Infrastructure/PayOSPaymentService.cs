using Contracts.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Riok.Mapperly.Abstractions;
using BadRequestException = Domain.Shared.Exceptions.HttpErrorExceptions.BadRequestException;
using InternalServerErrorException = Domain.Shared.Exceptions.HttpErrorExceptions.InternalServerErrorException;

namespace Infrastructure;

public class PayOSPaymentService(IConfiguration configuration, ILogger<PayOSPaymentService> logger) : IPayOSPaymentService
{
    private readonly PayOSClient _payOSClient = new(new PayOSOptions
    {
        ClientId = configuration["PayOS:ClientId"],
        ApiKey = configuration["PayOS:ApiKey"],
        ChecksumKey = configuration["PayOS:ChecksumKey"]
    });

    public async Task<CreatePayOSPaymentResponse> CreatePaymentAsync(CreatePayOSPaymentRequest request)
    {
        try
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                request.Description = orderCode.ToString();
            }

            if (request.Description.Length > 9)
            {
                request.Description = request.Description[..9];
            }

            var result = await _payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
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

            return new CreatePayOSPaymentResponse
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

    public async Task<PayOSWebhookProcessingResult> ProcessWebhookAsync(PayOSWebhookRequestDto request)
    {
        var checksumKey = configuration["PayOS:ChecksumKey"]?.Trim() ?? "";
        logger.LogWarning(
            "[PayOSPaymentService] ProcessWebhookAsync: OrderCode={OrderCode}, KeyLength={KeyLength}, Signature={Signature}",
            request.Data?.OrderCode,
            checksumKey.Length,
            request.Signature);

        try
        {
            var sdkRequest = request.MapToSdkRequest();
            var webhookData = await _payOSClient.Webhooks.VerifyAsync(sdkRequest);
            return new PayOSWebhookProcessingResult
            {
                PaymentReferenceId = webhookData.OrderCode.ToString()
            };
        }
        catch (WebhookException we)
        {
            logger.LogError(we,
                "[PayOSPaymentService] Xác thực webhook thất bại: {Message}. Signature={Signature}",
                we.Message,
                request.Signature);
            throw new BadRequestException($"Xác thực webhook PayOS thất bại: {we.Message}", we);
        }
        catch (Exception e)
        {
            logger.LogError(e, "[PayOSPaymentService] Xử lý webhook thất bại: {Message}", e.Message);
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
    [MapProperty(nameof(PayOSWebhookRequestDto.Desc), nameof(Webhook.Description))]
    [MapProperty(nameof(PayOSWebhookRequestDto.Data.Desc), nameof(Webhook.Data.Description2))]
    public static partial Webhook MapToSdkRequest(this PayOSWebhookRequestDto source);
}