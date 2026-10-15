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
public class DetailModel(ApiClient apiClient) : PageModel
{
    public JobDetailResponseDto? Job { get; private set; }
    public CanUpdateJobResponseDto? CanUpdateResult { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Job = await apiClient.Api.Employer.Jobs[id].GetAsync();
            if (Job == null)
            {
                TempData.SetErrorMessage("Không tìm thấy thông tin công việc.");
                return RedirectToPage("Index");
            }

            try
            {
                CanUpdateResult = await apiClient.Api.Employer.Jobs[id].CanUpdate.GetAsync();
            }
            catch
            {
                CanUpdateResult = new CanUpdateJobResponseDto
                {
                    CanUpdate = false,
                    Reason = "Không thể kiểm tra trạng thái cập nhật của công việc."
                };
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("Index");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin chi tiết công việc.");
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCloseJobAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.Jobs[id].Close.PutAsync();
            TempData.SetSuccessMessage("Đã đóng bài đăng công việc thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi đóng bài đăng công việc.");
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReopenJobAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.Jobs[id].Reopen.PutAsync();
            TempData.SetSuccessMessage("Đã mở lại bài đăng công việc thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi mở lại bài đăng công việc.");
        }

        return RedirectToPage(new { id });
    }

    public (string Label, string CssClass) GetStatusDisplay(JobStatus? status) => status switch
    {
        JobStatus.Active => ("Đang mở", "bg-success"),
        JobStatus.Closed => ("Đã đóng", "bg-secondary"),
        JobStatus.AdminHidden => ("Bị ẩn bởi Admin", "bg-warning text-dark"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };

    public (string Label, string CssClass) GetApplicationStatusDisplay(JobApplicationStatus? status) => status switch
    {
        JobApplicationStatus.Pending => ("Chờ duyệt", "bg-warning text-dark"),
        JobApplicationStatus.Accepted => ("Đã nhận", "bg-success"),
        JobApplicationStatus.Rejected => ("Đã từ chối", "bg-danger"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
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

    public string GetGenderDisplay(Gender? gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Không yêu cầu"
    };

    public string GetTimeUnitDisplay(JobSalaryTimeUnit? unit) => unit switch
    {
        JobSalaryTimeUnit.Hour => "Giờ",
        JobSalaryTimeUnit.Session => "Ca (Buổi)",
        JobSalaryTimeUnit.Day => "Ngày",
        JobSalaryTimeUnit.Week => "Tuần",
        JobSalaryTimeUnit.Month => "Tháng",
        JobSalaryTimeUnit.Project => "Dự án",
        _ => unit?.ToString() ?? "-"
    };

    public string FormatSalary(double? amount, int? quantity, JobSalaryTimeUnit? unit)
    {
        if (!amount.HasValue)
        {
            return "-";
        }

        var amountText = amount.Value.ToString("N0", new CultureInfo("vi-VN"));
        
        var unitText = unit switch
        {
            JobSalaryTimeUnit.Hour => "giờ",
            JobSalaryTimeUnit.Session => "ca",
            JobSalaryTimeUnit.Day => "ngày",
            JobSalaryTimeUnit.Week => "tuần",
            JobSalaryTimeUnit.Month => "tháng",
            JobSalaryTimeUnit.Project => "dự án",
            _ => unit?.ToString().ToLower() ?? "tháng"
        };

        var quantityText = quantity is > 1 ? $"{quantity} " : "";
        return $"{amountText} VND/{quantityText}{unitText}";
    }

    public string FormatSalaryRate(int? quantity, JobSalaryTimeUnit? unit)
    {
        var unitText = GetTimeUnitDisplay(unit);
        return quantity is > 1 ? $"{quantity} {unitText}" : unitText;
    }

    public string FormatSalaryAmount(double? amount)
    {
        return amount.HasValue ? amount.Value.ToString("N0", new CultureInfo("vi-VN")) + " VNĐ" : "-";
    }

    public string FormatDate(Date? date)
    {
        return date.HasValue ? $"{date.Value.Day:D2}/{date.Value.Month:D2}/{date.Value.Year}" : "-";
    }

    public string FormatTime(Time? time)
    {
        return time.HasValue ? $"{time.Value.Hour:D2}:{time.Value.Minute:D2}" : "-";
    }
}