using ApiSdk;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Frontend.ViewModels.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Manager.ViewJobStatsPackages;

[Authorize(Roles = Role.Manager)]
public class CreateModel(ApiClient apiClient) : PageModel
{
    [BindProperty]
    public ViewJobStatsPackageFormInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            await apiClient.Api.Manager.ViewJobStatsPackages.PostAsync(
                Input.ToRequest(), cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã tạo gói lượt xem.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ gói lượt xem. Vui lòng thử lại sau.");
        }

        return Page();
    }
}
