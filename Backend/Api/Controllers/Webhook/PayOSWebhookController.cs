using Contracts.Application;
using Contracts.Application.Employer;
using Contracts.External.Payment;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class PayOSWebhookController(
    IPaymentServiceFactory paymentServiceFactory,
    IPaymentTransactionService paymentTransactionService,
    ISubscriptionService subscriptionService,
    ILogger<PayOSWebhookController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessWebhook([FromBody] PayOSIpnRequest request)
    {
        var paymentService = paymentServiceFactory.Get(PaymentProvider.PayOS);
        var payOsResponse = await paymentService.ProcessIpnAsync(request) as PayOsIpnResponse ??
                            throw new InvalidOperationException("Phản hồi từ PayOS không hợp lệ.");

        await OnIpnVerifiedAsync(payOsResponse);
        return Ok();
    }

    private async Task OnIpnVerifiedAsync(PayOsIpnResponse response)
    {
        switch (await paymentTransactionService.GetPaymentPurposeAsync(response.PaymentReferenceId,
                    PaymentProvider.PayOS))
        {
            case PaymentPurpose.EmployerSubscription:
                await subscriptionService.ProcessIpnAsync(response.PaymentReferenceId);
                break;
            default:
                logger.LogWarning(
                    "Mục đích thanh toán không được hỗ trợ hoặc không tìm thấy giao dịch cho mã tham chiếu {PaymentReferenceId}",
                    response.PaymentReferenceId);
                break;
        }
    }
}