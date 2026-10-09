using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repository.Constants;
using Service.ApplicationServices;

namespace Api.Controllers.Candidate
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Role.Candidate)]
    public class EmployerRegistrationsController(EmployerRegistrationService employerRegistrationService) : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType<EmployerRegistrationResponseDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EmployerRegister([FromForm] EmployerRegistrationRequestDto dto)
        {
            var response = await employerRegistrationService.RegisterAsync(User, dto);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpGet]
        [ProducesResponseType<IEnumerable<EmployerRegistrationViewDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var response = await employerRegistrationService.GetMyRegistrationsAsync(User);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<EmployerRegistrationDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyRegistrationById(Guid id)
        {
            var response = await employerRegistrationService.GetMyRegistrationByIdAsync(User, id);
            return Ok(response);
        }

        [HttpPut("{id:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelRegistration(Guid id)
        {
            await employerRegistrationService.CancelAsync(User, id);
            return NoContent();
        }
    }
}
