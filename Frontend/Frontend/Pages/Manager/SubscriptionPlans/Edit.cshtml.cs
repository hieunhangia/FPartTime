using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Frontend.ViewModels.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Manager.SubscriptionPlans;

[Authorize(Roles = Role.Manager)]
public class EditModel(ApiClient apiClient) : PageModel
{
    public EmployerSubscriptionTier Tier { get; private set; }
    public EmployerSubscriptionDuration Duration { get; private set; }
    public bool PlanLoaded { get; private set; }

    [BindProperty]
    public SubscriptionPlanFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string tier, string duration,
        CancellationToken cancellationToken)
    {
        if (!SetPlanKey(tier, duration)) return NotFound();

        try
        {
            var plan = await apiClient.Api.Manager.SubscriptionPlans[tier][duration]
                .GetAsync(cancellationToken: cancellationToken);
            if (plan is null)
            {
                TempData.SetErrorMessage("Không tìm thấy gói đăng ký.");
                return RedirectToPage("./Index");
            }

            Input = SubscriptionPlanFormInput.FromResponse(plan);
            PlanLoaded = true;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            TempData.SetErrorMessage("Không tìm thấy gói đăng ký.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể tải gói đăng ký. Vui lòng thử lại sau.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string tier, string duration,
        CancellationToken cancellationToken)
    {
        if (!SetPlanKey(tier, duration)) return NotFound();
        PlanLoaded = true;
        if (!ModelState.IsValid) return Page();

        try
        {
            await apiClient.Api.Manager.SubscriptionPlans[tier][duration]
                .PutAsync(Input.ToUpdateRequest(), cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã cập nhật gói đăng ký.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            TempData.SetErrorMessage("Không tìm thấy gói đăng ký.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ gói đăng ký. Vui lòng thử lại sau.");
        }

        return Page();
    }

    private bool SetPlanKey(string tier, string duration)
    {
        if (!Enum.TryParse(tier, out EmployerSubscriptionTier parsedTier) ||
            !Enum.IsDefined(parsedTier) ||
            !Enum.TryParse(duration, out EmployerSubscriptionDuration parsedDuration) ||
            !Enum.IsDefined(parsedDuration))
        {
            return false;
        }

        Tier = parsedTier;
        Duration = parsedDuration;
        return true;
    }
}
