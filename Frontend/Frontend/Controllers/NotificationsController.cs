using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController(ApiClient apiClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int skip = 0, int top = 5)
    {
        try
        {
            return new JsonResult(new
            {
                success = true, data = await apiClient.Api.Notifications
                    .GetWithOdataAsync<NotificationResponseDto>(q => q
                        .Skip(skip)
                        .Top(top)
                        .OrderByDescending(n => n.CreatedAt)) ?? []
            });
        }
        catch (ApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.ToFriendlyErrorMessage() });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi lấy thông báo." });
        }
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            await apiClient.Api.Notifications[id].Read.PutAsync();
            return new JsonResult(new { success = true });
        }
        catch (ApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.ToFriendlyErrorMessage() });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi đánh dấu đã đọc." });
        }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            await apiClient.Api.Notifications.ReadAll.PutAsync();
            return new JsonResult(new { success = true });
        }
        catch (ApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.ToFriendlyErrorMessage() });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi đánh dấu tất cả đã đọc." });
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var count = await apiClient.Api.Notifications.UnreadCount.GetAsync();
            return new JsonResult(new { success = true, data = count ?? 0 });
        }
        catch (ApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.ToFriendlyErrorMessage() });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi lấy số lượng thông báo chưa đọc." });
        }
    }
}