using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Repository.Models.Users;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Api.Middlewares;

public class UserLockoutCheckingMiddleware(RequestDelegate next)
{
    private const string LockoutCachePrefix = "user_lockout_";

    private enum UserAccountStatus
    {
        NotFound,
        Locked,
        Active
    }

    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userId = context.User.GetUserId();
        var status = await cache.GetOrCreateAsync($"{LockoutCachePrefix}{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var user = await userManager.FindByIdAsync(userId.ToString());
            return user switch
            {
                null => UserAccountStatus.NotFound,
                { LockoutEnabled: true, LockoutEnd: not null } when user.LockoutEnd.Value > DateTimeOffset.UtcNow =>
                    UserAccountStatus.Locked,
                _ => UserAccountStatus.Active
            };
        });

        switch (status)
        {
            case UserAccountStatus.NotFound:
                throw new UnauthorizedException("Tài khoản người dùng không tồn tại hoặc đã bị xóa.");
            case UserAccountStatus.Locked:
                throw new ForbiddenException("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên.");
            case UserAccountStatus.Active:
            default:
                await next(context);
                break;
        }
    }
}

public static class UserLockoutCheckingMiddlewareExtensions
{
    public static void UseUserLockoutChecking(this IApplicationBuilder builder) =>
        builder.UseMiddleware<UserLockoutCheckingMiddleware>();
}