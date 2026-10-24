using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Candidate.CandidateApplication
{
    [Authorize(Roles = Role.Candidate)]
    public class ApplicationHistoryModel(ApiClient apiClient) : PageModel
    {
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? OrderBy { get; set; } = "AppliedAt_Desc";
        [BindProperty(SupportsGet = true)] public DateOnly? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateOnly? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 10;
        public List<JobApplicationResponseDto> Applications { get; private set; } = [];
        public long TotalCount { get; private set; }
        public int TotalPages { get; private set; } = 1;


        public async Task<IActionResult> OnGetAsync()
        {
            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
            {
                TempData.SetWarningMessage("Từ ngày không được lớn hơn đến ngày.");
                (FromDate, ToDate) = (ToDate, FromDate);
            }

            var skip = (PageNumber - 1) * PageSize;

            var dateClauses = new List<string>();
            if (FromDate.HasValue)
            {
                dateClauses.Add($"AppliedAt ge {FromDate.Value:yyyy-MM-dd}T00:00:00Z");
            }
            if (ToDate.HasValue)
            {
                dateClauses.Add($"AppliedAt le {ToDate.Value:yyyy-MM-dd}T23:59:59Z");
            }
            var dateFilter = dateClauses.Count > 0 ? string.Join(" and ", dateClauses) : null;

            try
            {
                var result = await apiClient.Api.Candidate.JobApplications
                    .GetPagedWithODataAsync<JobApplicationResponseDto, PagedResponseDtoOfJobApplicationResponseDto>(q =>
                    {
                        if (!string.IsNullOrWhiteSpace(Keyword))
                        {
                            var kw = Keyword.Trim();
                            q.Filter((a, f) => f.Contains(a.JobTitle, kw));
                        }
                        switch (OrderBy)
                        {
                            case "AppliedAt_Asc":
                                q.OrderBy(a => a.AppliedAt);
                                break;
                            case "JobTitle_Asc":
                                q.OrderBy(a => a.JobTitle);
                                break;
                            case "JobTitle_Desc":
                                q.OrderByDescending(a => a.JobTitle);
                                break;
                            default:
                                q.OrderByDescending(a => a.AppliedAt);
                                break;
                        }
                        q.Skip(skip).Top(PageSize);
                    }, additionalFilter: dateFilter);
                Applications = result?.Items ?? [];
                TotalCount = result?.TotalCount ?? 0;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                if (TotalPages == 0) TotalPages = 1;
                if (PageNumber > TotalPages) PageNumber = TotalPages;
            }
            catch (ApiException ex)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            }
            catch
            {
                TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách đơn ứng tuyển.");
            }
            return Page();
        }


        public (string Label, string CssClass) GetStatusDisplay(JobApplicationStatus? status) => status switch
        {
            JobApplicationStatus.Pending => ("Chờ duyệt", "bg-warning text-dark"),
            JobApplicationStatus.Accepted => ("Đã nhận", "bg-success"),
            JobApplicationStatus.Rejected => ("Đã từ chối", "bg-danger"),
            _ => (status?.ToString() ?? "-", "bg-secondary")
        };
    }
}
