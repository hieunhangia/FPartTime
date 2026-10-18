using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.Job;

[Authorize(Roles = Role.Employer)]
public class ApplicationDetailModel(ApiClient apiClient) : PageModel
{
    public JobApplicationDetailResponseDto? Application { get; private set; }

    [BindProperty(SupportsGet = true)] public Guid? JobId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Application = await apiClient.Api.Employer.Jobs.Applications[id].GetAsync();
            if (Application == null)
            {
                TempData.SetErrorMessage("Không tìm thấy thông tin đơn ứng tuyển.");
                return RedirectBack();
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectBack();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin chi tiết đơn ứng tuyển.");
            return RedirectBack();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.Jobs.Applications[id].Accept.PutAsync();
            TempData.SetSuccessMessage("Đã chấp nhận đơn ứng tuyển thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi chấp nhận đơn ứng tuyển.");
        }

        return RedirectToPage(new { id, jobId = JobId });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.Jobs.Applications[id].Reject.PutAsync();
            TempData.SetSuccessMessage("Đã từ chối đơn ứng tuyển.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi từ chối đơn ứng tuyển.");
        }

        return RedirectToPage(new { id, jobId = JobId });
    }

    private RedirectToPageResult RedirectBack() =>
        JobId.HasValue ? RedirectToPage("Detail", new { id = JobId.Value }) : RedirectToPage("Index");

    public (string Label, string CssClass) GetApplicationStatusDisplay(JobApplicationStatus? status) => status switch
    {
        JobApplicationStatus.Pending => ("Chờ duyệt", "bg-warning text-dark"),
        JobApplicationStatus.Accepted => ("Đã nhận", "bg-success"),
        JobApplicationStatus.Rejected => ("Đã từ chối", "bg-danger"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };

    public string GetGenderDisplay(Gender? gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Không xác định"
    };

    public string GetDayOfWeekDisplay(DayOfWeekObject? day) => day switch
    {
        DayOfWeekObject.Monday => "Thứ Hai",
        DayOfWeekObject.Tuesday => "Thứ Ba",
        DayOfWeekObject.Wednesday => "Thứ Tư",
        DayOfWeekObject.Thursday => "Thứ Năm",
        DayOfWeekObject.Friday => "Thứ Sáu",
        DayOfWeekObject.Saturday => "Thứ Bảy",
        DayOfWeekObject.Sunday => "Chủ Nhật",
        _ => day?.ToString() ?? "-"
    };

    public string FormatDate(Date? date)
    {
        return date.HasValue ? $"{date.Value.Day:D2}/{date.Value.Month:D2}/{date.Value.Year}" : "-";
    }
    
    public string GetShiftDisplay(Shift? shift) => shift switch
    {
        Shift.Morning => "Ca sáng",
        Shift.Afternoon => "Ca chiều",
        Shift.Evening => "Ca tối",
        Shift.Night => "Ca đêm",
        _ => "-"
    };

    public int? GetAge(Date? dob)
    {
        if (!dob.HasValue)
        {
            return null;
        }

        var today = DateTime.Today;
        var age = today.Year - dob.Value.Year;
        if (dob.Value.Month > today.Month || (dob.Value.Month == today.Month && dob.Value.Day > today.Day))
        {
            age--;
        }

        return age;
    }
}