using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Service.Censor;

namespace Api.Controllers.Censor;

[ApiController]
[Route("api/censor/job-reports")]
[Authorize(Roles = Role.Censor)]
public class JobReportsController(JobReportService jobReportService) : ControllerBase
{
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType<IEnumerable<CensorJobReportViewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<CensorJobReportViewDto> GetReports([FromQuery] JobReportStatus? status) =>
        jobReportService.GetReportsQueryable(status);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CensorJobReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CensorJobReportDetailDto>> GetReportById(Guid id)
    {
        var report = await jobReportService.GetReportByIdAsync(id);
        return Ok(report);
    }

    [HttpPut("{id:guid}/lock-job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LockJob(Guid id)
    {
        await jobReportService.LockJobAsync(User, id);
        return NoContent();
    }

    [HttpPut("{id:guid}/lock-employer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LockEmployer(Guid id)
    {
        await jobReportService.LockEmployerAsync(User, id);
        return NoContent();
    }

    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectReport(Guid id)
    {
        await jobReportService.RejectAsync(User, id);
        return NoContent();
    }
}