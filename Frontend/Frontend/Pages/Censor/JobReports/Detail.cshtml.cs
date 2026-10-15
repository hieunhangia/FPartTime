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
public class DetailModel(ApiClient apiClient) : PageModel
{
    public CensorJobReportDetailDto? Report { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Report = await apiClient.Api.Censor.JobReports[id].GetAsync();
            if (Report == null)
            {
                TempData.SetErrorMessage("Không tìm thấy đơn báo cáo.");
                return RedirectToPage("./Index");
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("./Index");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải chi tiết đơn báo cáo.");
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public Task<IActionResult> OnPostLockJobAsync(Guid id) =>
        ProcessReportAsync(id,
            () => apiClient.Api.Censor.JobReports[id].LockJob.PutAsync(),
            "Đã ẩn bài đăng.");

    public Task<IActionResult> OnPostLockEmployerAsync(Guid id) =>
        ProcessReportAsync(id,
            () => apiClient.Api.Censor.JobReports[id].LockEmployer.PutAsync(),
            "Đã khóa người đăng bài.");

    public Task<IActionResult> OnPostRejectAsync(Guid id) =>
        ProcessReportAsync(id,
            () => apiClient.Api.Censor.JobReports[id].Reject.PutAsync(),
            "Đã từ chối báo cáo.");

    private async Task<IActionResult> ProcessReportAsync(Guid id, Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            TempData.SetSuccessMessage(successMessage);
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi xử lý báo cáo.");
        }

        return RedirectToPage("./Detail", new { id });
    }

    public (string Label, string CssClass) GetStatusDisplay(JobReportStatus? status) => status switch
    {
        JobReportStatus.Pending => ("Chờ xử lý", "bg-warning text-dark"),
        JobReportStatus.Resolved => ("Đã giải quyết", "bg-success"),
        JobReportStatus.Dismissed => ("Đã bỏ qua", "bg-secondary"),
        _ => ("-", "bg-secondary")
    };
}

