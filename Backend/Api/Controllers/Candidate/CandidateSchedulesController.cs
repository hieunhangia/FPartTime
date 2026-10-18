using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Candidate;

namespace Api.Controllers.Candidate
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles =Role.Candidate)]
    public class CandidateSchedulesController(CandidateScheduleService scheduleService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<List<CandidateScheduleDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CandidateScheduleDto>>> GetMySchedules()
        {
            return Ok(await scheduleService.GetSchedulesAsync(User));
        }

        [HttpPut]
        [Consumes("application/json")]
        [ProducesResponseType<List<CandidateScheduleDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CandidateScheduleDto>>> UpdateMySchedules(
            [FromBody] UpdateCandidateScheduleDto request)
        {
            return Ok(await scheduleService.UpdateSchedulesAsync(User, request));
        }
    }
}
