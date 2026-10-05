using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Constants;
using Service.ApplicationServices.Admin;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Role.Admin)]
public class SystemHealthController(SystemHealthService systemHealthService) : ControllerBase
{
    [HttpGet("/api/health")]
    [ProducesResponseType<SystemHealthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemHealth(CancellationToken cancellationToken) =>
        Ok(await systemHealthService.GetSystemHealthAsync(cancellationToken));
}