using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Censor.JobReports;

[Authorize(Roles = Role.Censor)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<CensorJobReportViewDto> Reports { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public JobReportStatus? StatusFilter { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Reports = await apiClient.Api.Censor.JobReports.GetAsync(q =>
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
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách báo cáo.");
        }

        return Page();
    }

    public (string Label, string CssClass) GetStatusDisplay(JobReportStatus? status) => status switch
    {
        JobReportStatus.Pending => ("Chờ xử lý", "bg-warning text-dark"),
        JobReportStatus.Resolved => ("Đã giải quyết", "bg-success"),
        JobReportStatus.Dismissed => ("Đã bỏ qua", "bg-secondary"),
        _ => ("-", "bg-secondary")
    };
}

