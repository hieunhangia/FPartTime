using Contracts.Application.Manager;
using Contracts.Common;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Manager;

[ApiController]
[Route("api/manager/employers")]
[Authorize(Roles = Role.Manager)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class EmployersController(IEmployerManagementService employerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ManagedEmployerSummaryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEmployersAsync([FromQuery] EmployerListRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await employerService.GetEmployersAsync(request, cancellationToken));

    [HttpGet("{employerId:guid}")]
    [ProducesResponseType<ManagedEmployerDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployerAsync(Guid employerId, CancellationToken cancellationToken) =>
        Ok(await employerService.GetEmployerAsync(employerId, cancellationToken));
}
