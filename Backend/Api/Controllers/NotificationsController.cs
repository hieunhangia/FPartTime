using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Service.ApplicationServices;

namespace Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(NotificationsService notificationsService) : ControllerBase
{
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType<IEnumerable<NotificationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<NotificationResponseDto> GetMyNotifications() =>
        notificationsService.GetMyNotificationsQueryable(User);

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