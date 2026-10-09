using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repository.Constants;
using Repository.Models.Users;
using Service.ApplicationServices;

namespace Api.Controllers.Censor
{
    [Route("api/censor/employer-registrations")]
    [ApiController]
    [Authorize(Roles = Role.Censor)]
    public class EmployerRegistrationsController(CensorEmployerRegistrationService registrationService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IEnumerable<EmployerRegistrationViewDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRegistrations([FromQuery] EmployerRegistrationRequestStatus? status)
        {
            var response = await registrationService.GetRegistrationsAsync(status);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<EmployerRegistrationDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRegistrationById(Guid id)
        {
            var response = await registrationService.GetRegistrationByIdAsync(id);
            return Ok(response);
        }

        [HttpPut("{id:guid}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveRegistration(Guid id)
        {
            await registrationService.ApproveAsync(User, id);
            return NoContent();
        }

        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectRegistration(Guid id, [FromBody] RejectEmployerRegistrationRequestDto dto)
        {
            await registrationService.RejectAsync(User, id, dto.RejectReason);
            return NoContent();
        }
    }
}

