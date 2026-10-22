using System.Globalization;
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
public class RecommendedCandidatesModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public JobDetailResponseDto? Job { get; private set; }

    public List<RecommendedCandidateResponseDto> Candidates { get; private set; } = [];

    public long TotalCount { get; private set; }

    public int TotalPages { get; private set; } = 1;

    public bool HasPermissionError { get; private set; }

    public string? PermissionErrorMessage { get; private set; }

    public bool IsJobInactive { get; private set; }

    public string? InactiveJobMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (JobId == Guid.Empty)
        {
            TempData.SetErrorMessage("Mã công việc không hợp lệ.");
            return RedirectToPage("Index");
        }

        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        // 1. Tải thông tin công việc để hiển thị tiêu đề và đối chiếu tiêu chí
        try
        {
            Job = await apiClient.Api.Employer.Jobs[JobId].GetAsync();
            if (Job == null)
            {
                TempData.SetErrorMessage("Không tìm thấy thông tin công việc hoặc bạn không có quyền truy cập.");
                return RedirectToPage("Index");
            }

            if (Job.Status != JobStatus.Active)
            {
                IsJobInactive = true;
                InactiveJobMessage = "Chỉ có thể tìm ứng viên phù hợp cho công việc đang ở trạng thái hoạt động.";
                return Page();
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("Index");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin công việc.");
            return RedirectToPage("Index");
        }

        // 2. Tải danh sách ứng viên gợi ý phù hợp
        try
        {
            var result = await apiClient.Api.Employer.JobsEscaped[JobId].RecommendedCandidates.GetAsync(q =>
            {
                q.QueryParameters.PageNumber = PageNumber;
                q.QueryParameters.PageSize = PageSize;
            });

            Candidates = result?.Items ?? [];
            TotalCount = result?.TotalCount ?? 0;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 403)
        {
            HasPermissionError = true;
            PermissionErrorMessage = ex.ToFriendlyErrorMessage();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400)
        {
            IsJobInactive = true;
            InactiveJobMessage = ex.ToFriendlyErrorMessage();
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách ứng viên phù hợp.");
        }

        return Page();
    }

    public string GetGenderDisplay(Gender? gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Không yêu cầu"
    };

    public string GetCandidateGenderDisplay(Gender? gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Khác"
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

    public string GetShiftDisplay(Shift? shift) => shift switch
    {
        Shift.Morning => "Ca sáng",
        Shift.Afternoon => "Ca chiều",
        Shift.Evening => "Ca tối",
        Shift.Night => "Ca đêm",
        _ => "-"
    };

    public string FormatDate(Date? date) =>
        date.HasValue ? $"{date.Value.Day:D2}/{date.Value.Month:D2}/{date.Value.Year}" : "-";

    public string FormatTime(Time? time) =>
        time.HasValue ? $"{time.Value.Hour:D2}:{time.Value.Minute:D2}" : "-";

    public string GetScoreBadgeClass(int? score) => score switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-primary",
        _ => "bg-secondary"
    };
}
