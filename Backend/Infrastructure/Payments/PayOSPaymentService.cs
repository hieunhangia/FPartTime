using Contracts.External.Payment;
using Domain;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Riok.Mapperly.Abstractions;
using PaymentTransaction = Domain.Models.Payments.PaymentTransaction;

namespace Infrastructure.Payments;

public class PayOSPaymentService(PayOSClient payOSClient, ApplicationDbContext context) : IPaymentService
{
    public PaymentProvider Provider => PaymentProvider.PayOS;

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

        var transaction = new PaymentTransaction
        {
            UserId = request.UserId,
            Purpose = request.Purpose,
            Provider = Provider,
            PaymentReferenceId = result.OrderCode.ToString(),
            Amount = request.Amount,
            Status = PaymentTransactionStatus.Pending,
            Description = request.Description,
            ExpiredAt = request.ExpiredAt
        };
        context.PaymentTransactions.Add(transaction);
        await context.SaveChangesAsync();

        return new CreatePaymentResponse
        {
            PaymentTransactionId = transaction.Id,
            PaymentReferenceId = result.OrderCode.ToString(),
            PaymentUrl = result.CheckoutUrl
        };
    }

    public async Task<IIpnResponse> ProcessIpnAsync(IIpnRequest request)
    {
        var payOSRequest = request as PayOSIpnRequest ??
                           throw new BadRequestException("PayOSIpnRequest là bắt buộc khi sử dụng PayOS.");
        return (await payOSClient.Webhooks.VerifyAsync(payOSRequest.MapToSdkRequest())).MapToDomainResponse();
    }

    public async Task<PaymentPurpose?> GetPaymentPurposeAsync(string paymentReferenceId) =>
        await context.PaymentTransactions
            .Where(t => t.PaymentReferenceId == paymentReferenceId && t.Provider == Provider)
            .Select(t => t.Purpose)
            .FirstOrDefaultAsync();

    public async Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default) =>
        await context.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Pending && t.ExpiredAt < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired), cancellationToken);
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
    public static partial PayOsIpnResponse MapToDomainResponse(this WebhookData source);
}