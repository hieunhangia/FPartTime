using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Public;

public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<JobResponseDto> Jobs { get; private set; } = [];
    public List<string> Provinces { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Province { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var response = await apiClient.Api.Jobs.GetAsync();
            var allJobs = response?.Items ?? [];

            // Thu thập danh sách tỉnh/thành để lọc
            Provinces = allJobs
                .Where(j => !string.IsNullOrWhiteSpace(j.ProvinceName))
                .Select(j => j.ProvinceName!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var query = allJobs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var term = Keyword.Trim();
                query = query.Where(j =>
                    (j.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.EmployerCompanyName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.IndustryName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(Province))
            {
                query = query.Where(j => string.Equals(j.ProvinceName?.Trim(), Province.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            Jobs = query.ToList();
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách việc làm.");
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

    public static string FormatSalariesSummary(List<JobSalaryResponseDto>? salaries)
    {
        if (salaries == null || salaries.Count == 0)
        {
            return "Thỏa thuận";
        }

        if (salaries.Count == 1)
        {
            return FormatSalary(salaries[0]);
        }

        return $"{FormatSalary(salaries[0])} (+{salaries.Count - 1} mức lương)";
    }

    public static string FormatScheduleSummary(List<JobScheduleResponseDto>? schedules)
    {
        if (schedules == null || schedules.Count == 0)
        {
            return "Linh hoạt";
        }

        var count = schedules.Count;
        return $"{count} buổi/ca trong tuần";
    }

    public static string FormatLocation(JobResponseDto job)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(job.CommuneName))
        {
            parts.Add(job.CommuneName.Trim());
        }
        if (!string.IsNullOrWhiteSpace(job.ProvinceName))
        {
            parts.Add(job.ProvinceName.Trim());
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "Toàn quốc";
    }
}