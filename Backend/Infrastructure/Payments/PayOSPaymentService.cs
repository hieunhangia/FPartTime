using Contracts.External.Payment;
using Domain.Shared.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Riok.Mapperly.Abstractions;
using BadRequestException = Domain.Shared.Exceptions.HttpErrorExceptions.BadRequestException;
using InternalServerErrorException = Domain.Shared.Exceptions.HttpErrorExceptions.InternalServerErrorException;

namespace Infrastructure.Payments;

public class PayOSPaymentService(PayOSClient payOSClient) : IPaymentService
{
    public PaymentProvider Provider => PaymentProvider.PayOS;

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var payOsCreatePaymentRequest = request as PayOSCreatePaymentRequest ?? throw new BadRequestException(
            $"{nameof(PayOSCreatePaymentRequest)} là bắt buộc khi sử dụng PayOS.");
        try
        {
            var result = await payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
            {
                OrderCode = payOsCreatePaymentRequest.OrderCode,
                Amount = decimal.ToInt64(payOsCreatePaymentRequest.Amount),
                Description = payOsCreatePaymentRequest.Description,
                CancelUrl = payOsCreatePaymentRequest.CancelUrl,
                ReturnUrl = payOsCreatePaymentRequest.ReturnUrl,
                BuyerPhone = payOsCreatePaymentRequest.BuyerPhone,
                ExpiredAt = payOsCreatePaymentRequest.ExpiredAt.HasValue
                    ? new DateTimeOffset(payOsCreatePaymentRequest.ExpiredAt.Value.ToUniversalTime())
                        .ToUnixTimeSeconds()
                    : null,
                Items =
                [
                    .. payOsCreatePaymentRequest.Items.Select(i => new PaymentLinkItem
                    {
                        Name = i.Name,
                        Price = decimal.ToInt64(i.Price),
                        Quantity = i.Quantity
                    })
                ]
            });

            return new PayOSCreatePaymentResponse
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
            await payOSClient.PaymentRequests.CancelAsync(paymentReferenceId);
        }
        catch (Exception e)
        {
            throw new InternalServerErrorException($"Hủy yêu cầu thanh toán PayOS thất bại: {e.Message}", e);
        }
    }

    public async Task<IpnResponse> ProcessIpnAsync(IpnRequest request)
    {
        var payOsIpnRequest = request as PayOSIpnRequest ?? throw new BadRequestException(
            $"{nameof(PayOSIpnRequest)} là bắt buộc khi sử dụng PayOS.");
        try
        {
            return (await payOSClient.Webhooks.VerifyAsync(payOsIpnRequest.MapToSdkRequest())).MapToDomainResponse();
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
    public static partial Webhook MapToSdkRequest(this PayOSIpnRequest source);

    [MapProperty(nameof(WebhookData.OrderCode), nameof(PayOsIpnResponse.PaymentReferenceId))]
    public static partial PayOsIpnResponse MapToDomainResponse(this WebhookData source);
}