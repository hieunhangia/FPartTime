using System.ComponentModel.DataAnnotations;
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
public class CreateModel(ApiClient apiClient) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Vui lòng chọn hạng gói.")]
    [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Hạng gói không hợp lệ.")]
    public EmployerSubscriptionTier? Tier { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng chọn kỳ hạn.")]
    [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn không hợp lệ.")]
    public EmployerSubscriptionDuration? Duration { get; set; }

    [BindProperty]
    public SubscriptionPlanFormInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            await apiClient.Api.Manager.SubscriptionPlans.PostAsync(
                Input.ToCreateRequest(Tier!.Value, Duration!.Value), cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã tạo gói đăng ký.");
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
}
