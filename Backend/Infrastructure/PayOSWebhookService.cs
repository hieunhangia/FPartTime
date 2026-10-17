using Contracts;
using PayOS;
using PayOS.Models.Webhooks;

namespace Infrastructure;

public class PayOSWebhookService(PayOSClient payOSClient) : IPaymentIpnService<Webhook, WebhookData>
{
    public async Task<WebhookData> ProcessIpnAsync(Webhook request) => await payOSClient.Webhooks.VerifyAsync(request);
}