using Contracts.Application;
using Contracts.External.Payments;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class IpnController(IPaymentServiceFactory paymentServiceFactory, IIpnDispatcher ipnDispatcher)
    : ControllerBase
{
    [HttpPost("payos")]
    public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookRequestDto request)
    {
        try
        {
            var result = await paymentServiceFactory.Get(PaymentProvider.PayOS).ProcessIpnAsync(request);
            if (!result.IsValidSignature)
            {
                return BadRequest();
            }

            if (result.IsPaymentSuccess && !string.IsNullOrEmpty(result.PaymentReferenceId))
            {
                await ipnDispatcher.DispatchAsync(result.PaymentReferenceId);
            }

            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet("vnpay")]
    public async Task<IActionResult> VNPAYIpn([FromQuery] VNPAYIpnRequestDto request)
    {
        try
        {
            var result = await paymentServiceFactory.Get(PaymentProvider.VNPAY).ProcessIpnAsync(request);
            if (!result.IsValidSignature)
            {
                return Ok(new VNPAYIpnResponseDto
                {
                    RspCode = "97",
                    Message = "Invalid signature"
                });
            }

            if (result.IsPaymentSuccess && !string.IsNullOrEmpty(result.PaymentReferenceId))
            {
                await ipnDispatcher.DispatchAsync(result.PaymentReferenceId);
            }

            return Ok(new VNPAYIpnResponseDto
            {
                RspCode = "00",
                Message = "Confirm Success"
            });
        }
        catch (Exception ex)
        {
            return Ok(new VNPAYIpnResponseDto
            {
                RspCode = "99",
                Message = ex.Message
            });
        }
    }
}