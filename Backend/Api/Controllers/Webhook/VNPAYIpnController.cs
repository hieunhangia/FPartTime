using Contracts.Application;
using Contracts.External;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class VNPAYIpnController(IVNPAYPaymentService vnpayPaymentService, IIpnDispatcher ipnDispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<VNPAYIpnResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessIpn([FromQuery] VNPAYIpnRequestDto request)
    {
        try
        {
            var processResult = await vnpayPaymentService.ProcessIpnAsync(request);
            if (!processResult.IsValidSignature)
            {
                return Ok(new VNPAYIpnResponseDto
                {
                    RspCode = "97",
                    Message = "Invalid Checksum"
                });
            }

            if (processResult.IsPaymentSuccess)
            {
                await ipnDispatcher.DispatchAsync(processResult.PaymentReferenceId);
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