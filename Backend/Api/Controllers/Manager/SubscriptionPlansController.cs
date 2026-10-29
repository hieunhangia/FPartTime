using Contracts.Application.Manager;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Manager;

[ApiController]
[Route("api/manager/subscription-plans")]
[Authorize(Roles = Role.Manager)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class SubscriptionPlansController(ISubscriptionPlanManagementService planService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ManagedSubscriptionPlanResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlansAsync(CancellationToken cancellationToken) =>
        Ok(await planService.GetPlansAsync(cancellationToken));

    [HttpGet("{tier}/{duration}", Name = "ManagerSubscriptionPlanDetail")]
    [ProducesResponseType<ManagedSubscriptionPlanResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, CancellationToken cancellationToken) =>
        Ok(await planService.GetPlanAsync(tier, duration, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ManagedSubscriptionPlanResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlanAsync([FromBody] CreateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        var plan = await planService.CreatePlanAsync(request, cancellationToken);
        return CreatedAtRoute("ManagerSubscriptionPlanDetail", new { plan.Tier, plan.Duration }, plan);
    }

    [HttpPut("{tier}/{duration}")]
    [ProducesResponseType<ManagedSubscriptionPlanResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, [FromBody] UpdateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await planService.UpdatePlanAsync(tier, duration, request, cancellationToken));

    [HttpDelete("{tier}/{duration}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, CancellationToken cancellationToken)
    {
        await planService.DeactivatePlanAsync(tier, duration, cancellationToken);
        return NoContent();
    }
}
