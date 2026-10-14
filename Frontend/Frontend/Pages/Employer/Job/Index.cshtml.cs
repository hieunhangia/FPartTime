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
public class IndexModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)] public JobStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)] public string? OrderBy { get; set; } = "CreatedAt_Desc";

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public List<JobSummaryResponseDto> Jobs { get; private set; } = [];

    public long TotalCount { get; private set; }

    public int TotalPages { get; private set; } = 1;

    public PostsQuotaResponseDto? PostsQuota { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        var skip = (PageNumber - 1) * PageSize;

        try
        {
            PostsQuota = await apiClient.Api.Jobs.PostsQuota.GetAsync();
        }
        catch
        {
            PostsQuota = null;
        }

        try
        {
            var result = await apiClient.Api.Jobs
                .GetPagedWithODataAsync<JobSummaryResponseDto, PagedResponseDtoOfJobSummaryResponseDto>(q =>
                {
                    if (!string.IsNullOrWhiteSpace(Keyword) && StatusFilter.HasValue)
                    {
                        var kw = Keyword.Trim();
                        q.Filter((j, f) => f.Contains(j.Title, kw) && j.Status == StatusFilter.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(Keyword))
                    {
                        var kw = Keyword.Trim();
                        q.Filter((j, f) => f.Contains(j.Title, kw));
                    }
                    else if (StatusFilter.HasValue)
                    {
                        q.Filter(j => j.Status == StatusFilter.Value);
                    }

                    switch (OrderBy)
                    {
                        case "CreatedAt_Asc":
                            q.OrderBy(j => j.CreatedAt);
                            break;
                        case "Title_Asc":
                            q.OrderBy(j => j.Title);
                            break;
                        case "Title_Desc":
                            q.OrderByDescending(j => j.Title);
                            break;
                        default:
                            q.OrderByDescending(j => j.CreatedAt);
                            break;
                    }

                    q.Skip(skip).Top(PageSize);
                });

            Jobs = result?.Items ?? [];
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
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách công việc.");
        }

        return Page();
    }

    public (string Label, string CssClass) GetStatusDisplay(JobStatus? status) => status switch
    {
        JobStatus.Active => ("Đang mở", "bg-success"),
        JobStatus.Closed => ("Đã đóng", "bg-secondary"),
        JobStatus.AdminHidden => ("Bị ẩn bởi Admin", "bg-warning text-dark"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };

    public string FormatMaxSalary(List<JobSalaryResponseDto>? salaries)
    {
        if (salaries == null || salaries.Count == 0)
        {
            return "Thỏa thuận";
        }

        var maxSalary = salaries.OrderByDescending(s => s.Amount ?? 0).FirstOrDefault();
        if (maxSalary?.Amount == null)
        {
            return "Thỏa thuận";
        }

        var amountText = maxSalary.Amount.Value.ToString("N0", new CultureInfo("vi-VN"));
        var unitText = maxSalary.TimeUnit switch
        {
            JobSalaryTimeUnit.Hour => "giờ",
            JobSalaryTimeUnit.Session => "ca",
            JobSalaryTimeUnit.Day => "ngày",
            JobSalaryTimeUnit.Week => "tuần",
            JobSalaryTimeUnit.Month => "tháng",
            JobSalaryTimeUnit.Project => "dự án",
            _ => "tháng"
        };

        var quantityText = maxSalary.TimeQuantity is > 1 ? $"{maxSalary.TimeQuantity} " : "";

        return $"Đến {amountText} VNĐ/{quantityText}{unitText}";
    }
}