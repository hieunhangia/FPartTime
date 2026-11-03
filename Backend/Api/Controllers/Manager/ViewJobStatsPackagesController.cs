using Contracts.Application.Manager;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Manager;

[ApiController]
[Route("api/manager/view-job-stats-packages")]
[Authorize(Roles = Role.Manager)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class ViewJobStatsPackagesController(IViewJobStatsPackageManagementService packageService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ManagedViewJobStatsPackageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackagesAsync(CancellationToken cancellationToken) =>
        Ok(await packageService.GetPackagesAsync(cancellationToken));

    [HttpGet("{id:guid}", Name = "ManagerViewJobStatsPackageDetail")]
    [ProducesResponseType<ManagedViewJobStatsPackageResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackageAsync(Guid id, CancellationToken cancellationToken) =>
        Ok(await packageService.GetPackageAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ManagedViewJobStatsPackageResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePackageAsync([FromBody] CreateUpdateViewJobStatsPackageRequestDto request,
        CancellationToken cancellationToken)
    {
        var package = await packageService.CreatePackageAsync(request, cancellationToken);
        return CreatedAtRoute("ManagerViewJobStatsPackageDetail", new { package.Id }, package);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ManagedViewJobStatsPackageResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePackageAsync(Guid id,
        [FromBody] CreateUpdateViewJobStatsPackageRequestDto request, CancellationToken cancellationToken) =>
        Ok(await packageService.UpdatePackageAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePackageAsync(Guid id, CancellationToken cancellationToken)
    {
        await packageService.DeactivatePackageAsync(id, cancellationToken);
        return NoContent();
    }
}
