using Frontend.Constants;
using Frontend.Extensions;
using Frontend.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Frontend.Pages.Admin.SystemHealth;

[Authorize(Roles = Role.Admin)]
public class DetailModel(SystemHealthService systemHealthService, IMemoryCache memoryCache) : PageModel
{
    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(BusinessRuleConstants.SystemHealth.CacheDuration);

    public MonitorDetailData? DetailData { get; private set; }

    [BindProperty(SupportsGet = true)] public string? FromDate { get; set; }

    [BindProperty(SupportsGet = true)] public string? ToDate { get; set; }

    public async Task<IActionResult> OnGetAsync(string id, bool refresh = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToPage("Index");
        }

        var cacheKey = $"BetterStack_Monitor_{id}_Detail";
        if (!refresh && string.IsNullOrEmpty(FromDate) && string.IsNullOrEmpty(ToDate) &&
            memoryCache.TryGetValue(cacheKey, out MonitorDetailData? cached) && cached != null)
        {
            DetailData = cached;
            return Page();
        }

        try
        {
            var detail = await systemHealthService.GetMonitorDetailAsync(id, FromDate, ToDate);
            if (detail == null)
            {
                TempData.SetErrorMessage("Dữ liệu dịch vụ không hợp lệ.");
                return Page();
            }

            DetailData = detail;

            if (string.IsNullOrEmpty(FromDate) && string.IsNullOrEmpty(ToDate))
            {
                memoryCache.Set(cacheKey, DetailData, CacheDuration);
            }

            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi kết nối đến dịch vụ giám sát.");
            return Page();
        }
    }
}