using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;

namespace Service.ExternalServices;

public class PayOSService(PayOSClient payOSClient)
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await payOSClient.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
        {
            OrderCode = request.OrderCode,
            Amount = request.Amount,
            Description = request.Description,
            Items =
            [
                .. request.Items.Select(item => new PaymentLinkItem
                {
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                })
            ],
            CancelUrl = request.CancelUrl,
            ReturnUrl = request.ReturnUrl,
            ExpiredAt = request.ExpiredAt,
            BuyerName = request.BuyerName,
            BuyerEmail = request.BuyerEmail,
            BuyerPhone = request.BuyerPhone
        }, new RequestOptions<CreatePaymentLinkRequest> { CancellationToken = cancellationToken });
        return new CreatePaymentResponse
        {
            PaymentLinkId = response.PaymentLinkId,
            CheckoutUrl = response.CheckoutUrl
        };
    }
}

public class PayOSHealthCheck(PayOSClient payOSClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await payOSClient.GetAsync<object>("/", new RequestOptions<object>
            {
                MaxRetries = 0,
                CancellationToken = cancellationToken
            });

            return HealthCheckResult.Healthy("Kết nối đến PayOS thành công.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Kết nối đến PayOS thất bại.", ex);
        }
    }
}

public class CreatePaymentRequest
{
    public required long OrderCode { get; set; }
    public required long Amount { get; set; }
    public required string Description { get; set; }
    public required List<PaymentItem> Items { get; set; }
    public required string CancelUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public long? ExpiredAt { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
}

public class PaymentItem
{
    public required string Name { get; set; }
    public required long Price { get; set; }
    public required int Quantity { get; set; }
}

public class CreatePaymentResponse
{
    public required string PaymentLinkId { get; set; }
    public required string CheckoutUrl { get; set; }
}