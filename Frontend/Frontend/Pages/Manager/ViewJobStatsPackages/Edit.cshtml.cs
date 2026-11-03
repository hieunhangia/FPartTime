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
public class EditModel(ApiClient apiClient) : PageModel
{
    public bool PackageLoaded { get; private set; }

    [BindProperty]
    public ViewJobStatsPackageFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return NotFound();

        try
        {
            var package = await apiClient.Api.Manager.ViewJobStatsPackages[id]
                .GetAsync(cancellationToken: cancellationToken);
            if (package is null)
            {
                TempData.SetErrorMessage("Không tìm thấy gói lượt xem.");
                return RedirectToPage("./Index");
            }

            Input = ViewJobStatsPackageFormInput.FromResponse(package);
            PackageLoaded = true;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            TempData.SetErrorMessage("Không tìm thấy gói lượt xem.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể tải gói lượt xem. Vui lòng thử lại sau.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return NotFound();
        PackageLoaded = true;
        if (!ModelState.IsValid) return Page();

        try
        {
            await apiClient.Api.Manager.ViewJobStatsPackages[id]
                .PutAsync(Input.ToRequest(), cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã cập nhật gói lượt xem.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            TempData.SetErrorMessage("Không tìm thấy gói lượt xem.");
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
