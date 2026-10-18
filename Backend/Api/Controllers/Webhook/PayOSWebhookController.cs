using Contracts.External.Payment;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class PayOSWebhookController(IPaymentServiceFactory paymentServiceFactory)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessWebhook([FromBody] PayOSIpnRequest request)
    {
        var response = await paymentServiceFactory.Get(PaymentProvider.PayOS).ProcessIpnAsync(request);
        var payOsResponse = response as PayOsIpnResponse ??
                            throw new InvalidOperationException("Phản hồi từ PayOS không hợp lệ.");
        await OnIpnVerifiedAsync(payOsResponse);
        return Ok();
    }

    // This method is called when the webhook is verified successfully.
    private async Task OnIpnVerifiedAsync(PayOsIpnResponse response)
    {
        var paymentReferenceId = response.PaymentReferenceId;
    }
}