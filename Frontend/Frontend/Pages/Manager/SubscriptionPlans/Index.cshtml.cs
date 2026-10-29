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

namespace Frontend.Pages.Manager.SubscriptionPlans;

[Authorize(Roles = Role.Manager)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<ManagedSubscriptionPlanResponseDto> Plans { get; private set; } = [];
    public bool PlansLoaded { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPlansAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string tier, string duration,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EmployerSubscriptionTier>(tier, out var parsedTier) ||
            !Enum.IsDefined(parsedTier) ||
            !Enum.TryParse<EmployerSubscriptionDuration>(duration, out var parsedDuration) ||
            !Enum.IsDefined(parsedDuration))
        {
            ModelState.AddModelError(string.Empty, "Không xác định được gói cần ngừng cung cấp.");
            await LoadPlansAsync(cancellationToken);
            return Page();
        }

        try
        {
            await apiClient.Api.Manager.SubscriptionPlans[tier][duration]
                .DeleteAsync(cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã ngừng cung cấp gói đăng ký.");
            return RedirectToPage();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ gói đăng ký. Vui lòng thử lại sau.");
        }

        await LoadPlansAsync(cancellationToken);
        return Page();
    }

    public static string FormatPrice(double? price) => price is null
        ? "—"
        : price.Value.ToString(price.Value % 1 == 0 ? "N0" : "N2", new CultureInfo("vi-VN")) + " ₫";

    private async Task LoadPlansAsync(CancellationToken cancellationToken)
    {
        try
        {
            Plans = await apiClient.Api.Manager.SubscriptionPlans.GetAsync(cancellationToken: cancellationToken) ?? [];
            PlansLoaded = true;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể tải danh sách gói đăng ký. Vui lòng thử lại sau.");
        }
    }
}
