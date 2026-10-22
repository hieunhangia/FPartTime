using Contracts.Application.Candidate;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Candidate;

namespace Api.Controllers.Candidate
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Role.Candidate)]
    public class CandidateProfilesController(
        CandidateProfileService profileService,
        ICandidateProfileService candidateprofileService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<CandidateProfileResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CandidateProfileResponseDto>> GetMyProfile()
        {
            return Ok(await profileService.GetProfileAsync(User));
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        [ProducesResponseType<CandidateProfileResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CandidateProfileResponseDto>> EditMyProfile(
            [FromForm] UpdateCandidateProfileDto request)
        {
            return Ok(await profileService.EditCandidateProfile(User, request));
        }

        [HttpGet("open-for-job-status")]
        [ProducesResponseType<OpenForJobStatusResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OpenForJobStatusResponseDto>> GetOpenForJobStatus()
        {
            return Ok(await candidateprofileService.GetOpenForJobStatusAsync(User));
        }

        [HttpPut("open-for-job-status")]
        [ProducesResponseType<OpenForJobStatusResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OpenForJobStatusResponseDto>> ToggleOpenForJobStatus(
            [FromBody] ToggleOpenForJobStatusRequestDto request)
        {
            return Ok(await candidateprofileService.ToggleOpenForJobStatusAsync(User, request));
        }
    }
}