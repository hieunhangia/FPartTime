using Contracts.Application.Candidate;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Candidate;

[ApiController]
[Route("api/candidate/view-job-stats")]
[Authorize(Roles = Role.Candidate)]
public class CandidateViewJobStatsController(ICandidateViewJobStatsService service) : ControllerBase
{
    [HttpGet("balance")]
    [ProducesResponseType<CandidateViewJobStatsBalanceResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBalance() =>
        Ok(await service.GetBalanceAsync(User));

    [HttpGet("packages")]
    [ProducesResponseType<List<CandidateViewJobStatsPackageResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPackages() =>
        Ok(await service.GetPackagesAsync());

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType<CandidateJobStatsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobStats(Guid jobId) =>
        Ok(await service.GetJobStatsAsync(User, jobId));

    [HttpPost("jobs/{jobId:guid}/unlock")]
    [ProducesResponseType<CandidateJobStatsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnlockOrRefreshJobStats(Guid jobId) =>
        Ok(await service.UnlockOrRefreshJobStatsAsync(User, jobId));

    [HttpPost("orders/payos-checkout")]
    [ProducesResponseType<CandidateViewJobStatsCheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayOSCheckout([FromBody] CreateCandidateViewPayOSCheckoutRequestDto request) =>
        Ok(await service.CreatePayOSCheckoutAsync(User, request));

    [HttpPost("orders/vnpay-checkout")]
    [ProducesResponseType<CandidateViewJobStatsCheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateVNPAYCheckout([FromBody] CreateCandidateViewVNPAYCheckoutRequestDto request) =>
        Ok(await service.CreateVNPAYCheckoutAsync(User,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1", request));

    [HttpPost("orders/zalopay-checkout")]
    [ProducesResponseType<CandidateViewJobStatsCheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateZaloPayCheckout([FromBody] CreateCandidateViewZaloPayCheckoutRequestDto request) =>
        Ok(await service.CreateZaloPayCheckoutAsync(User, request));
}
