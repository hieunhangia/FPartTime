using System.Security.Claims;
using Contracts.Application;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Models.Notifications;
using Riok.Mapperly.Abstractions;

namespace Application;

public class NotificationsService(ApplicationDbContext dbContext) : INotificationsService
{
    public async Task CreateNotificationAsync(Guid userId, string title, string content)
    {
        dbContext.Notifications.Add(new Notification
        {
            Title = title,
            Content = content,
            UserId = userId
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task CreateNotificationsAsync(List<Guid> userIds, string title, string content)
    {
        dbContext.Notifications.AddRange(userIds.Select(userId => new Notification
        {
            Title = title,
            Content = content,
            UserId = userId
        }));
        await dbContext.SaveChangesAsync();
    }

    public IQueryable<NotificationResponseDto> GetMyNotificationsQueryable(ClaimsPrincipal user) =>
        dbContext.Notifications.AsNoTracking()
            .Where(n => n.UserId == user.GetUserId())
            .ProjectToDto();

    public async Task<int> GetUnreadCountAsync(ClaimsPrincipal user) =>
        await dbContext.Notifications.AsNoTracking().CountAsync(n => n.UserId == user.GetUserId() && !n.IsRead);

    public async Task MarkAsReadAsync(ClaimsPrincipal user, Guid notificationId)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == user.GetUserId());

        if (notification == null)
        {
            throw new NotFoundException("Không tìm thấy thông báo.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(ClaimsPrincipal user) =>
        await dbContext.Notifications
            .Where(n => n.UserId == user.GetUserId() && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class NotificationMapper
{
    public static partial IQueryable<NotificationResponseDto> ProjectToDto(this IQueryable<Notification> notifications);
}