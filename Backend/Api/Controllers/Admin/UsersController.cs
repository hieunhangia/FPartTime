using Contracts.Application.Admin;
using Contracts.Common;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = Role.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] UserListRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await userService.GetUsersAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken) =>
        Ok(await userService.GetUserAsync(id, cancellationToken));

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType<UserDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateRolesAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/lockout")]
    [ProducesResponseType<UserDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await userService.LockUserAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}/lockout")]
    [ProducesResponseType<UserDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken) =>
        Ok(await userService.UnlockUserAsync(id, cancellationToken));
}
