using System.Globalization;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Manager.ViewJobStatsPackages;

[Authorize(Roles = Role.Manager)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<ManagedViewJobStatsPackageResponseDto> Packages { get; private set; } = [];
    public bool PackagesLoaded { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPackagesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Không xác định được gói cần ngừng cung cấp.");
            await LoadPackagesAsync(cancellationToken);
            return Page();
        }

        try
        {
            await apiClient.Api.Manager.ViewJobStatsPackages[id].DeleteAsync(cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã ngừng cung cấp gói lượt xem.");
            return RedirectToPage();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ gói lượt xem. Vui lòng thử lại sau.");
        }

        await LoadPackagesAsync(cancellationToken);
        return Page();
    }

    public static string FormatPrice(double? price) => price is null
        ? "—"
        : price.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    private async Task LoadPackagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Packages = await apiClient.Api.Manager.ViewJobStatsPackages.GetAsync(
                cancellationToken: cancellationToken) ?? [];
            PackagesLoaded = true;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể tải danh sách gói lượt xem. Vui lòng thử lại sau.");
        }
    }
}
