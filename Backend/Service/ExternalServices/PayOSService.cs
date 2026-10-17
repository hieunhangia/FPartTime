using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

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

    public async Task<PaymentWebhookDataRequest> VerifyWebhookAsync(PaymentWebhookRequest request)
    {
        var response = await payOSClient.Webhooks.VerifyAsync(new Webhook
        {
            Code = request.Code,
            Description = request.Desc,
            Success = request.Success,
            Data = new WebhookData
            {
                OrderCode = request.Data.OrderCode,
                Amount = request.Data.Amount,
                Description = request.Data.Description,
                AccountNumber = request.Data.AccountNumber,
                Reference = request.Data.Reference,
                TransactionDateTime = request.Data.TransactionDateTime,
                Currency = request.Data.Currency,
                PaymentLinkId = request.Data.PaymentLinkId,
                Code = request.Data.Code,
                Description2 = request.Data.Desc
            },
            Signature = request.Signature
        });
        return new PaymentWebhookDataRequest
        {
            OrderCode = response.OrderCode,
            Amount = response.Amount,
            Description = response.Description,
            AccountNumber = response.AccountNumber,
            Reference = response.Reference,
            TransactionDateTime = response.TransactionDateTime,
            Currency = response.Currency,
            PaymentLinkId = response.PaymentLinkId,
            Code = response.Code,
            Desc = response.Description2
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

public class PaymentWebhookRequest
{
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Desc { get; set; } = string.Empty;
    [Required] public bool Success { get; set; }
    [Required] public PaymentWebhookDataRequest Data { get; set; } = new();
    [Required] public string Signature { get; set; } = string.Empty;
}

public class PaymentWebhookDataRequest
{
    [Required] public long OrderCode { get; set; }
    [Required] public long Amount { get; set; }
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public string AccountNumber { get; set; } = string.Empty;
    [Required] public string Reference { get; set; } = string.Empty;
    [Required] public string TransactionDateTime { get; set; } = string.Empty;
    [Required] public string Currency { get; set; } = string.Empty;
    [Required] public string PaymentLinkId { get; set; } = string.Empty;
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Desc { get; set; } = string.Empty;
}