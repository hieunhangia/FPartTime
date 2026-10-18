using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Employer;

namespace Api.Controllers.Employer
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Role.Employer)]
    public class EmployerProfilesController(EmployerProfileService profileService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<EmployerProfileResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployerProfileResponseDto>> GetMyProfile()
        {
            return Ok(await profileService.GetProfileAsync(User));
        }

        [HttpPut]
        [Consumes("multipart/form-data")]
        [ProducesResponseType<EmployerProfileResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployerProfileResponseDto>> EditMyProfile(
            [FromForm] UpdateEmployerProfileDto request)
        {
            return Ok(await profileService.EditEmployerProfile(User, request));
        }
    }
}
