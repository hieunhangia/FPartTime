using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Public;

public class JobDetailModel(ApiClient apiClient) : PageModel
{
    public JobResponseDto? Job { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string BackUrl => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Public/Jobs")!;

    [BindProperty]
    public ReportJobInputModel ReportInput { get; set; } = new();

    public async Task<IActionResult> OnPostReportJobAsync(Guid id)
    {
        if (!User.IsLoggedIn)
        {
            TempData.SetWarningMessage("Vui lòng đăng nhập để báo cáo tin tuyển dụng.");
            return RedirectToPage("/Public/JobDetail", new { id, ReturnUrl });
        }

        if (!User.IsInRole(Role.Candidate))
        {
            TempData.SetErrorMessage("Chỉ tài khoản ứng viên mới có quyền báo cáo tin tuyển dụng.");
            return RedirectToPage("/Public/JobDetail", new { id, ReturnUrl });
        }

        if (string.IsNullOrWhiteSpace(ReportInput.Reason) || ReportInput.Reason.Trim().Length < 3)
        {
            TempData.SetErrorMessage("Vui lòng nhập lý do báo cáo hợp lệ (tối thiểu 3 ký tự).");
            return RedirectToPage("/Public/JobDetail", new { id, ReturnUrl });
        }

        if (ReportInput.Reason.Length > 500)
        {
            TempData.SetErrorMessage("Lý do báo cáo không được vượt quá 500 ký tự.");
            return RedirectToPage("/Public/JobDetail", new { id, ReturnUrl });
        }

        try
        {
            var requestDto = new CreateJobReportDto
            {
                JobId = id,
                Reason = ReportInput.Reason.Trim()
            };

            await apiClient.Api.Candidate.JobReports.PostAsync(requestDto);
            TempData.SetSuccessMessage("Báo cáo của bạn đã được gửi thành công và đang chờ xử lý.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi gửi báo cáo tin tuyển dụng.");
        }

        return RedirectToPage("/Public/JobDetail", new { id, ReturnUrl });
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Job = await apiClient.Api.Jobs[id].GetAsync();
            if (Job == null)
            {
                TempData.SetErrorMessage("Không tìm thấy thông tin công việc hoặc công việc không còn khả dụng.");
                return RedirectToPage("/Public/Jobs");
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("/Public/Jobs");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin chi tiết công việc.");
            return RedirectToPage("/Public/Jobs");
        }

        return Page();
    }

    public static string FormatSalary(JobSalaryResponseDto salary)
    {
        var amountStr = (salary.Amount ?? 0).ToString("N0") + " ₫";
        var unitStr = salary.TimeUnit switch
        {
            JobSalaryTimeUnit.Hour => "giờ",
            JobSalaryTimeUnit.Session => "ca",
            JobSalaryTimeUnit.Day => "ngày",
            JobSalaryTimeUnit.Week => "tuần",
            JobSalaryTimeUnit.Month => "tháng",
            JobSalaryTimeUnit.Project => "dự án",
            _ => "lần"
        };

        if (salary.TimeQuantity is > 1)
        {
            return $"{amountStr} / {salary.TimeQuantity} {unitStr}";
        }

        return $"{amountStr} / {unitStr}";
    }

    public static string FormatDayOfWeek(DayOfWeekObject? day) => day switch
    {
        DayOfWeekObject.Monday => "Thứ Hai",
        DayOfWeekObject.Tuesday => "Thứ Ba",
        DayOfWeekObject.Wednesday => "Thứ Tư",
        DayOfWeekObject.Thursday => "Thứ Năm",
        DayOfWeekObject.Friday => "Thứ Sáu",
        DayOfWeekObject.Saturday => "Thứ Bảy",
        DayOfWeekObject.Sunday => "Chủ Nhật",
        _ => "-"
    };

    public static string FormatTime(Time? time)
    {
        if (time == null) return "-";
        return time.ToString() ?? "-";
    }

    public static string FormatAgeRequirement(int? minAge, int? maxAge)
    {
        if (minAge.HasValue && maxAge.HasValue)
        {
            return $"{minAge.Value} - {maxAge.Value} tuổi";
        }
        if (minAge.HasValue)
        {
            return $"Từ {minAge.Value} tuổi trở lên";
        }
        if (maxAge.HasValue)
        {
            return $"Tối đa {maxAge.Value} tuổi";
        }
        return "Không yêu cầu độ tuổi";
    }

    public static string FormatGenderRequirement(Gender? gender)
    {
        if (gender == null) return "Không yêu cầu";
        return gender switch
        {
            Gender.Male => "Nam",
            Gender.Female => "Nữ",
            _ => "Không yêu cầu"
        };
    }

    public static string FormatFullAddress(JobResponseDto job)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(job.DetailAddress))
        {
            parts.Add(job.DetailAddress.Trim());
        }
        if (!string.IsNullOrWhiteSpace(job.CommuneName))
        {
            parts.Add(job.CommuneName.Trim());
        }
        if (!string.IsNullOrWhiteSpace(job.ProvinceName))
        {
            parts.Add(job.ProvinceName.Trim());
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "Không xác định";
    }
}

public class ReportJobInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập lý do báo cáo.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Lý do báo cáo phải từ 3 đến 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

