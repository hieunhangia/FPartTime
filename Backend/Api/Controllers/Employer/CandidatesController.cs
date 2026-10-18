using Api.Attributes;
using Contracts.Application.Employer;
using Contracts.Common;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Employer;

[ApiController]
[Route("api/employer/[controller]")]
[Authorize(Roles = Role.Employer)]
public class CandidatesController(ICandidateService candidateService) : ControllerBase
{
    [HttpGet("/api/employer/jobs/{jobId:guid}/recommended-candidates")]
    [HttpGet("recommended/{jobId:guid}")]
    [ProducesResponseType<PagedResponseDto<RecommendedCandidateResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult>
        GetRecommendedCandidates(Guid jobId, [FromQuery] PaginationRequestDto pagination) =>
        Ok(await candidateService.GetRecommendedCandidatesAsync(User, jobId, pagination));

    [HttpGet("search")]
    [ODataEnableQuery(OutputType.Paged)]
    [ProducesResponseType<PagedResponseDto<CandidateSummaryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IQueryable<CandidateSummaryResponseDto>> SearchCandidates() =>
        await candidateService.SearchCandidatesQueryableAsync(User);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CandidateDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCandidateDetail(Guid id) =>
        Ok(await candidateService.GetCandidateDetailAsync(User, id));
}