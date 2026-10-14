using Api.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Constants;
using Service;
using Service.ApplicationServices.Employer;

namespace Api.Controllers.Employer;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Role.Employer)]
public class JobsController(JobService jobService) : ControllerBase
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobDetail(Guid id) => Ok(await jobService.GetJobDetailAsync(User, id));

    [HttpGet("applications/{id:guid}")]
    [ProducesResponseType<JobApplicationDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobApplicationDetail(Guid id) =>
        Ok(await jobService.GetJobApplicationDetailAsync(User, id));

    [HttpGet("posts-quota")]
    [ProducesResponseType<PostsQuotaResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPostsQuota() => Ok(await jobService.GetPostsQuotaAsync(User));

    [HttpPost]
    [ProducesResponseType<JobDetailResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequestDto createJobRequestDto)
    {
        var result = await jobService.CreateJobAsync(User, createJobRequestDto);
        return CreatedAtAction(nameof(GetJobDetail), new { result.Id }, result);
    }

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
}