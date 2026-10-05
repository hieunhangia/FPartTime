using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.ApplicationServices;
using Service.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(NotificationsService notificationsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<NotificationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] PagedRequestDto requestDto) =>
        Ok(await notificationsService.GetMyNotificationsAsync(User, requestDto));

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await notificationsService.MarkAsReadAsync(User, id);
        return NoContent();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await notificationsService.MarkAllAsReadAsync(User);
        return NoContent();
    }
}