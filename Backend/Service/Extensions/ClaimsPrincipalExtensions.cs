using System.Security.Claims;
using Service.HttpErrorExceptions;

namespace Service.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetUserId()
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedException("Người dùng chưa đăng nhập hoặc thông tin không hợp lệ.");
            }

            return userId;
        }
    }
}