using Api.Attributes;
using Contracts.Common;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Service.Candidate;

namespace Api.Controllers.Candidate
{
    [Route("api/candidate/job-applications")]
    [ApiController]
    [Authorize(Roles =Role.Candidate)]
    public class JobApplicationsController(JobApplicationService service) : ControllerBase
    {
        [HttpPost("~/api/candidate/jobs/{jobId:guid}/apply")]
        [ProducesResponseType<JobApplicationResponseDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApplyJob(Guid jobId)
        {
            var result = await service.ApplyJobAsync(User, jobId);
            return CreatedAtAction(nameof(GetMyApplications), null, result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> WithdrawApplication(Guid id)
        {
            await service.WithdrawAsync(User, id);
            return NoContent();
        }


        [HttpGet]
        [ODataEnableQuery(OutputType.Paged)]
        [ProducesResponseType<PagedResponseDto<JobApplicationResponseDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public IQueryable<JobApplicationResponseDto> GetMyApplications() =>
        service.GetAllApplication(User);
    }
}
