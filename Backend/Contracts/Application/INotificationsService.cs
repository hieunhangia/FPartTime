using System.Security.Claims;

namespace Contracts.Application;

public interface INotificationsService
{
    Task CreateNotificationAsync(Guid userId, string title, string content);
    Task CreateNotificationsAsync(List<Guid> userIds, string title, string content);
    IQueryable<NotificationResponseDto> GetMyNotificationsQueryable(ClaimsPrincipal user);
    Task<int> GetUnreadCountAsync(ClaimsPrincipal user);
    Task MarkAsReadAsync(ClaimsPrincipal user, Guid notificationId);
    Task MarkAllAsReadAsync(ClaimsPrincipal user);
}

public class NotificationResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required bool IsRead { get; set; }
    public required DateTime CreatedAt { get; set; }
}