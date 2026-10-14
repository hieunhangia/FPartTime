using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Repository.Constants;
using Repository.Models.Jobs;
using Service.ApplicationServices.Censor;

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
}

