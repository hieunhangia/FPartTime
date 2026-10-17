using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Repository.Constants;
using Service.ApplicationServices.Candidate;
namespace Api.Controllers.Candidate
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Role.Candidate)]
    public class CandidateProfilesController(CandidateProfileService profileService) : ControllerBase
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
    }
}
