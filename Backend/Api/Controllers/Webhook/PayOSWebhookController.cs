using Contracts.Application;
using Contracts.External;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class PayOSWebhookController(IPayOSPaymentService payOsPaymentService, IIpnDispatcher ipnDispatcher)
    : ControllerBase
{
    [HttpPost]
    public async Task ProcessWebhook([FromBody] PayOSWebhookRequestDto request) =>
        await ipnDispatcher.DispatchAsync((await payOsPaymentService.ProcessWebhookAsync(request)).PaymentReferenceId);
}