using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Notifications;
using Riok.Mapperly.Abstractions;
using Service.DTOs;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices;

public class NotificationsService(ApplicationDbContext dbContext)
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

    public async Task CreateNotificationAsync(List<Guid> userIds, string title, string content)
    {
        dbContext.Notifications.AddRange(userIds.Select(userId => new Notification
        {
            Title = title,
            Content = content,
            UserId = userId
        }));
        await dbContext.SaveChangesAsync();
    }

    public async Task<PagedResponseDto<NotificationResponseDto>> GetMyNotificationsAsync(ClaimsPrincipal user,
        PagedRequestDto requestDto)
    {
        var query = dbContext.Notifications.Where(n => n.UserId == user.GetUserId());
        var totalCount = await query.CountAsync();
        if (totalCount == 0)
        {
            return new PagedResponseDto<NotificationResponseDto>([], 0);
        }

        return new PagedResponseDto<NotificationResponseDto>(
            await query.ApplyPaging(requestDto.PageIndex, requestDto.PageSize).ProjectToDto().ToListAsync(),
            totalCount);
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
public static partial class NotificationMapper
{
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