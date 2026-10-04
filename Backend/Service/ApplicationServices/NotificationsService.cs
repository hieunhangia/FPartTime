using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Notifications;
using Riok.Mapperly.Abstractions;
using Service.DTOs;
using Service.Extensions;
using Service.HttpErrorExceptions;
using Service.Hubs;

namespace Service.ApplicationServices;

public class NotificationsService(ApplicationDbContext dbContext, IHubContext<NotificationHub> hubContext)
{
    public async Task CreateAndSendNotificationAsync(Guid userId, string title, string content)
    {
        var notification = new Notification
        {
            Title = title,
            Content = content,
            UserId = userId
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        await hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification.MapToDto());
    }

    public async Task<PagedResponse<NotificationResponseDto>> GetMyNotificationsAsync(ClaimsPrincipal user,
        PagedRequest request)
    {
        var query = dbContext.Notifications.Where(n => n.UserId == user.GetUserId());
        var totalCount = await query.CountAsync();
        if (totalCount == 0)
        {
            return new PagedResponse<NotificationResponseDto>([], 0);
        }

        return new PagedResponse<NotificationResponseDto>(
            await query.ApplyPaging(request.PageIndex, request.PageSize).ProjectToDto().ToListAsync(), totalCount);
    }

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
public static partial class Mapper
{
    public static partial NotificationResponseDto MapToDto(this Notification notification);
    public static partial IQueryable<NotificationResponseDto> ProjectToDto(this IQueryable<Notification> notifications);
}

public class NotificationResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required bool IsRead { get; set; }
    public required DateTime CreatedAt { get; set; }
}