using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Repository.Constants;
using Service.ApplicationServices.Candidate;

namespace Api.Controllers.Candidate;

[ApiController]
[Route("api/candidate/job-reports")]
[Authorize(Roles = Role.Candidate)]
public class JobReportsController(JobReportService jobReportService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<JobReportResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJobReport([FromBody] CreateJobReportDto dto)
    {
        var result = await jobReportService.CreateReportAsync(User, dto);
        return CreatedAtAction(nameof(GetMyReportById), new { id = result.Id }, result);
    }

    [HttpPost("~/api/candidate/jobs/{jobId:guid}/report")]
    [ProducesResponseType<JobReportResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReportJob(Guid jobId, [FromBody] ReportJobReasonDto dto)
    {
        var result = await jobReportService.CreateReportAsync(User, new CreateJobReportDto
        {
            JobId = jobId,
            Reason = dto.Reason
        });
        return CreatedAtAction(nameof(GetMyReportById), new { id = result.Id }, result);
    }

    [HttpGet]
    [EnableQuery]
    [ProducesResponseType<IEnumerable<JobReportResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<JobReportResponseDto> GetMyReports() =>
        jobReportService.GetMyReportsQueryable(User);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobReportResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobReportResponseDto>> GetMyReportById(Guid id)
    {
        var report = await jobReportService.GetMyReportByIdAsync(User, id);
        return Ok(report);
    }
}

