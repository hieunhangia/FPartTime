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
public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<EmployerRegistrationViewDto> Registrations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? StatusFilter { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Registrations = await apiClient.Api.Censor.EmployerRegistrations.GetAsync(q =>
            {
                if (StatusFilter.HasValue)
                {
                    q.QueryParameters.Status = StatusFilter.Value;
                }
            }) ?? [];
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
}
