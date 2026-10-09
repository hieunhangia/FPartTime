using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Censor.EmployerRegistrations;

[Authorize(Roles = Role.Censor)]
public class DetailModel(ApiClient apiClient) : PageModel
{
    public EmployerRegistrationDetailDto? Registration { get; private set; }

    [BindProperty]
    public RejectEmployerRegistrationRequestDto RejectDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Registration = await apiClient.Api.Censor.EmployerRegistrations[id].GetAsync();
            if (Registration == null)
            {
                return NotFound();
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("./Index");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải chi tiết yêu cầu.");
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Censor.EmployerRegistrations[id].Approve.PutAsync();
            TempData.SetSuccessMessage("Đã duyệt yêu cầu thành công.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi duyệt yêu cầu.");
        }

        return RedirectToPage("./Detail", new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetErrorMessage("Vui lòng nhập lý do từ chối hợp lệ.");
            return RedirectToPage("./Detail", new { id });
        }

        try
        {
            await apiClient.Api.Censor.EmployerRegistrations[id].Reject.PutAsync(RejectDto);
            TempData.SetSuccessMessage("Đã từ chối yêu cầu.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi từ chối yêu cầu.");
        }

        return RedirectToPage("./Detail", new { id });
    }

    public (string Label, string CssClass) GetStatusDisplay(string? status) => status switch
    {
        "Pending" => ("Chờ duyệt", "bg-warning text-dark"),
        "Approved" => ("Đã duyệt", "bg-success"),
        "Rejected" => ("Đã từ chối", "bg-danger"),
        "Cancelled" => ("Đã hủy", "bg-secondary"),
        _ => (status ?? "-", "bg-secondary")
    };
}

