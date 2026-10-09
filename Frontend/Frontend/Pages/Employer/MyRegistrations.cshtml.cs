using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer;

[Authorize(Roles = Role.Candidate)]
public class MyRegistrationsModel(ApiClient apiClient) : PageModel
{
    public List<EmployerRegistrationViewDto> Registrations { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Registrations = await apiClient.Api.EmployerRegistrations.GetAsync() ?? [];
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách yêu cầu đăng ký.");
        }

        return Page();
    }
    public (string Label, string CssClass) GetStatusDisplay(string? status) => status switch
    {
        "Pending" => ("Chờ duyệt", "bg-warning text-dark"),
        "Approved" => ("Đã duyệt", "bg-success"),
        "Rejected" => ("Đã từ chối", "bg-danger"),
        "Cancelled" => ("Đã hủy", "bg-secondary"),
        _ => (status ?? "-", "bg-secondary")
    };

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        try
        {
            await apiClient.Api.EmployerRegistrations[id].Cancel.PutAsync();
            TempData.SetSuccessMessage("Đã hủy yêu cầu đăng ký thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi hủy yêu cầu đăng ký.");
        }

        return RedirectToPage();
    }
}
