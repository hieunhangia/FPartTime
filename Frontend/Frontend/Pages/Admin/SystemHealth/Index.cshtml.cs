using Frontend.Constants;
using Frontend.Extensions;
using Frontend.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Frontend.Pages.Admin.SystemHealth;

[Authorize(Roles = Role.Admin)]
public class IndexModel(SystemHealthService systemHealthService, IMemoryCache memoryCache)
    : PageModel
{
    private const string CacheKey = "SystemHealth_Cache";

    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(BusinessRuleConstants.SystemHealth.CacheDuration);

    public SystemHealthData? HealthData { get; set; }

    public async Task<IActionResult> OnGetAsync(bool refresh = false)
    {
        if (!refresh && memoryCache.TryGetValue(CacheKey, out SystemHealthData? cached) && cached != null)
        {
            HealthData = cached;
            return Page();
        }

        try
        {
            var result = await systemHealthService.GetSystemHealthDataAsync();
            memoryCache.Set(CacheKey, result, CacheDuration);
            HealthData = result;
        }
        catch
        {
            if (HealthData == null && memoryCache.TryGetValue(CacheKey, out SystemHealthData? fallback))
            {
                HealthData = fallback;
            }

            TempData.SetErrorMessage("Đã xảy ra lỗi khi kết nối đến dịch vụ giám sát.");
        }

        return Page();
    }
}