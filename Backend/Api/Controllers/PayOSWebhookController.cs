using Contracts;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayOSWebhookController(IPaymentIpnService<Webhook, WebhookData> paymentService) : ControllerBase
{
    [HttpPost("process")]
    public async Task ProcessIpn(Webhook webhook) =>
        await OnWebhookVerifiedAsync((await paymentService.ProcessIpnAsync(webhook)));

    // This method is called when the webhook is verified successfully.
    private async Task OnWebhookVerifiedAsync(WebhookData webhookData)
    {
        Console.WriteLine($"Webhook verified. OrderCode: {webhookData.OrderCode}");
        Console.WriteLine("Do something with the verified webhook data.");
    }
}