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
public class MyRegistrationDetailModel(ApiClient apiClient) : PageModel
{
    public EmployerRegistrationDetailDto? Registration { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("./MyRegistrations");
        }

        try
        {
            Registration = await apiClient.Api.EmployerRegistrations[id].GetAsync();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            TempData.SetErrorMessage("Không tìm thấy yêu cầu đăng ký.");
            return RedirectToPage("./MyRegistrations");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("./MyRegistrations");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải chi tiết yêu cầu đăng ký.");
            return RedirectToPage("./MyRegistrations");
        }

        if (Registration is null)
        {
            TempData.SetErrorMessage("Không tìm thấy yêu cầu đăng ký.");
            return RedirectToPage("./MyRegistrations");
        }

        return Page();
    }

    public string FormatDate(Date? date) => date is { } d ? ((DateOnly)d).ToString("dd/MM/yyyy") : "-";

    public string FormatDateTime(DateTimeOffset? dateTime) =>
        dateTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-";

    public (string Label, string CssClass) GetStatusDisplay(string? status) => status switch
    {
        "Pending" => ("Chờ duyệt", "bg-warning text-dark"),
        "Approved" => ("Đã duyệt", "bg-success"),
        "Rejected" => ("Đã từ chối", "bg-danger"),
        _ => (status ?? "-", "bg-secondary")
    };
}
