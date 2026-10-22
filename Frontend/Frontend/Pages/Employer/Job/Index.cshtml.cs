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

    [BindProperty(SupportsGet = true)] public string? OrderBy { get; set; } = "UpdatedAt_Desc";

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public List<JobSummaryResponseDto> Jobs { get; private set; } = [];

    public long TotalCount { get; private set; }

    public int TotalPages { get; private set; } = 1;

    public PostsQuotaResponseDto? PostsQuota { get; private set; }

    public CurrentEmployerSubscriptionResponseDto? CurrentSubscription { get; private set; }

    public bool HasActiveSubscription => CurrentSubscription?.HasActiveSubscription ?? false;

    public async Task<IActionResult> OnGetAsync()
    {
        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        var skip = (PageNumber - 1) * PageSize;

        try
        {
            PostsQuota = await apiClient.Api.Employer.Jobs.PostsQuota.GetAsync();
        }
        catch
        {
            PostsQuota = null;
        }

        try
        {
            CurrentSubscription = await apiClient.Api.Employer.Subscriptions.Current.GetAsync();
        }
        catch
        {
            CurrentSubscription = null;
        }

        try
        {
            var result = await apiClient.Api.Employer.Jobs
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
                        case "UpdatedAt_Asc":
                            q.OrderBy(j => j.UpdatedAt);
                            break;
                        case "Title_Asc":
                            q.OrderBy(j => j.Title);
                            break;
                        case "Title_Desc":
                            q.OrderByDescending(j => j.Title);
                            break;
                        default:
                            q.OrderByDescending(j => j.UpdatedAt);
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
}