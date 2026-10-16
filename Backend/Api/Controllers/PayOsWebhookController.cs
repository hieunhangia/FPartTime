using Microsoft.AspNetCore.Mvc;
using Service.ExternalServices;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayOsWebhookController(PayOSService payOSService) : ControllerBase
{
    [HttpPost("payment")]
    public async Task VerifyPayment(PaymentWebhookRequest request) =>
        await OnWebhookVerified(await payOSService.VerifyWebhookAsync(request));

    // This method is called when the webhook is verified successfully.
    private async Task OnWebhookVerified(PaymentWebhookDataRequest webhookData)
    {
        Console.WriteLine($"Webhook verified. OrderCode: {webhookData.OrderCode}");
        Console.WriteLine("Do something with the verified webhook data.");
    }
}