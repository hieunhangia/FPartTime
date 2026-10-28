using Contracts.Application.Manager;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Manager;

[ApiController]
[Route("api/manager/employers/{employerId:guid}/subscriptions")]
[Authorize(Roles = Role.Manager)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class EmployerSubscriptionsController(IEmployerSubscriptionManagementService subscriptionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ManagedEmployerSubscriptionResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionsAsync(Guid employerId, CancellationToken cancellationToken) =>
        Ok(await subscriptionService.GetSubscriptionsAsync(employerId, cancellationToken));

    [HttpGet("{subscriptionId:guid}", Name = "ManagerEmployerSubscriptionDetail")]
    [ProducesResponseType<ManagedEmployerSubscriptionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionAsync(Guid employerId, Guid subscriptionId,
        CancellationToken cancellationToken) =>
        Ok(await subscriptionService.GetSubscriptionAsync(employerId, subscriptionId, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ManagedEmployerSubscriptionResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantAsync(Guid employerId,
        [FromBody] GrantEmployerSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.GrantAsync(employerId, request, cancellationToken);
        return CreatedAtRoute("ManagerEmployerSubscriptionDetail",
            new { employerId, subscriptionId = result.Id }, result);
    }

    [HttpPatch("{subscriptionId:guid}/period")]
    [ProducesResponseType<ManagedEmployerSubscriptionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePeriodAsync(Guid employerId, Guid subscriptionId,
        [FromBody] UpdateEmployerSubscriptionPeriodRequestDto request, CancellationToken cancellationToken) =>
        Ok(await subscriptionService.UpdatePeriodAsync(employerId, subscriptionId, request,
            cancellationToken));

    [HttpPut("{subscriptionId:guid}/tier")]
    [ProducesResponseType<ManagedEmployerSubscriptionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTierAsync(Guid employerId, Guid subscriptionId,
        [FromBody] UpdateEmployerSubscriptionTierRequestDto request, CancellationToken cancellationToken) =>
        Ok(await subscriptionService.UpdateTierAsync(employerId, subscriptionId, request,
            cancellationToken));

    [HttpDelete("{subscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeAsync(Guid employerId, Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await subscriptionService.RevokeAsync(employerId, subscriptionId, cancellationToken);
        return NoContent();
    }
}
