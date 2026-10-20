using Contracts.Application.Employer;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Employer;

[ApiController]
[Route("api/employer/[controller]")]
[Authorize(Roles = Role.Employer)]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet("plans")]
    [ProducesResponseType<List<EmployerSubscriptionPlanResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlans() => Ok(await subscriptionService.GetPlansAsync());

    [HttpGet("current")]
    [ProducesResponseType<CurrentEmployerSubscriptionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentSubscription() =>
        Ok(await subscriptionService.GetCurrentSubscriptionAsync(User));

    [HttpPost("payos-checkout")]
    [ProducesResponseType<CreatePayOSCheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayOSCheckout([FromBody] CreatePayOSCheckoutRequestDto request) =>
        Ok(await subscriptionService.CreatePayOSCheckoutAsync(User, request));

    [HttpPut("checkout/{transactionId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelCheckout(Guid transactionId)
    {
        await subscriptionService.CancelPayOSCheckoutAsync(User, transactionId);
        return NoContent();
    }

    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelCurrentSubscription()
    {
        await subscriptionService.CancelCurrentSubscriptionAsync(User);
        return NoContent();
    }
}