using Contracts.External;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace Infrastructure;

public class PayOSPaymentService(PayOSClient payOSClient) : IPaymentService
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var options = request.AdditionalData as PayOSRequestAdditionalData ??
                      throw new BadRequestException("PayOSRequestAdditionalData là bắt buộc khi sử dụng PayOS.");
        var result = await payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = options.OrderCode,
            Amount = decimal.ToInt64(request.Amount),
            Description = request.Description,
            CancelUrl = request.CancelUrl,
            ReturnUrl = request.ReturnUrl,
            BuyerPhone = options.BuyerPhone,
            ExpiredAt = request.ExpiredAt.HasValue
                ? new DateTimeOffset(request.ExpiredAt.Value.ToUniversalTime()).ToUnixTimeSeconds()
                : null,
            Items =
            [
                .. options.Items.Select(i => new PaymentLinkItem
                {
                    Name = i.Name,
                    Price = decimal.ToInt64(i.Price),
                    Quantity = i.Quantity
                })
            ]
        });
        return new CreatePaymentResponse
        {
            PaymentUrl = result.CheckoutUrl
        };
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