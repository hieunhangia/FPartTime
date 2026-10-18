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
public class JobsController(IJobService jobService) : ControllerBase
{
    [HttpGet]
    [ODataEnableQuery(OutputType.Paged)]
    [ProducesResponseType<PagedResponseDto<JobSummaryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<JobSummaryResponseDto> GetMyCreatedJobs() => jobService.GetMyCreatedJobsQueryable(User);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobDetail(Guid id) => Ok(await jobService.GetJobDetailAsync(User, id));

    [HttpGet("posts-quota")]
    [ProducesResponseType<PostsQuotaResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPostsQuota() => Ok(await jobService.GetPostsQuotaAsync(User));

    [HttpPost]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJob([FromBody] CreateUpdateJobRequestDto createUpdateJobRequestDto)
    {
        var result = await jobService.CreateJobAsync(User, createUpdateJobRequestDto);
        return CreatedAtAction(nameof(GetJobDetail), new { result.Id }, result);
    }

    [HttpGet("{id:guid}/can-update")]
    [ProducesResponseType<CanUpdateJobResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanUpdateJob(Guid id) => Ok(await jobService.CanUpdateJobAsync(User, id));

    [HttpGet("{id:guid}/detail-for-update")]
    [ProducesResponseType<JobDetailForUpdateResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobDetailForUpdate(Guid id) =>
        Ok(await jobService.GetJobDetailForUpdateAsync(User, id));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult>
        UpdateJob(Guid id, [FromBody] CreateUpdateJobRequestDto createUpdateJobRequestDto) =>
        Ok(await jobService.UpdateJobAsync(User, id, createUpdateJobRequestDto));

    [HttpPut("{id:guid}/close")]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CloseJob(Guid id) => Ok(await jobService.CloseJobAsync(User, id));

    [HttpPut("{id:guid}/reopen")]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReopenJob(Guid id) => Ok(await jobService.ReopenJobAsync(User, id));

    [HttpGet("applications/{id:guid}")]
    [ProducesResponseType<JobApplicationDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobApplicationDetail(Guid id) =>
        Ok(await jobService.GetJobApplicationDetailAsync(User, id));

    [HttpPut("applications/{id:guid}/accept")]
    [ProducesResponseType<JobApplicationDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptJobApplication(Guid id) =>
        Ok(await jobService.AcceptJobApplicationAsync(User, id));

    [HttpPut("applications/{id:guid}/reject")]
    [ProducesResponseType<JobApplicationDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectJobApplication(Guid id) =>
        Ok(await jobService.RejectJobApplicationAsync(User, id));
}