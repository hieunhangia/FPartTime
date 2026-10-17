using Contracts;
using Contracts.HttpErrorExceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace Infrastructure;

public class PayOSPaymentService(PayOSClient payOSClient) : IPaymentService
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var response = await payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = request.AdditionalData?[nameof(CreatePaymentLinkRequest.OrderCode)] as long? ??
                        throw new BadRequestException(
                            $"{nameof(CreatePaymentLinkRequest.OrderCode)} là bắt buộc trong AdditionalData."),
            Amount = decimal.ToInt64(request.Amount),
            Description = request.Description,
            Items =
            [
                .. request.Items.Select(item => new PaymentLinkItem
                {
                    Name = item.Name,
                    Price = decimal.ToInt64(item.Price),
                    Quantity = item.Quantity
                })
            ],
            CancelUrl = request.CancelUrl,
            ReturnUrl = request.ReturnUrl,
            ExpiredAt = request.ExpiredAt.HasValue
                ? new DateTimeOffset(request.ExpiredAt.Value.ToUniversalTime()).ToUnixTimeSeconds()
                : null,
            BuyerName = request.AdditionalData?[nameof(CreatePaymentLinkRequest.BuyerName)] as string,
            BuyerEmail = request.AdditionalData?[nameof(CreatePaymentLinkRequest.BuyerEmail)] as string,
            BuyerPhone = request.AdditionalData?[nameof(CreatePaymentLinkRequest.BuyerPhone)] as string
        });
        return new CreatePaymentResponse
        {
            PaymentUrl = response.CheckoutUrl,
            AdditionalData = new Dictionary<string, object>
            {
                { nameof(response.PaymentLinkId), response.PaymentLinkId }
            }
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