using Api.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Service.Public;

namespace Api.Controllers.Public;

[ApiController]
[Route("api/jobs")]
[AllowAnonymous]
public class JobsController(JobService jobService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<JobResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<JobResponseDto> GetActiveJobs() =>
        jobService.GetActiveJobsQueryable();

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobResponseDto>> GetActiveJobById(Guid id)
    {
        var job = await jobService.GetActiveJobByIdAsync(id);
        return Ok(job);
    }
}

