using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Public;

public class JobsModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet = true)] public string? Province { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? IndustryId { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public int PageSize => 10;
    public List<JobResponseDto> Jobs { get; private set; } = [];
    public List<IndustryResponseDto> Industries { get; private set; } = [];
    public List<string> Provinces { get; private set; } = [];
    public long TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int FirstPage => Math.Max(1, Math.Min(PageNumber - 2, TotalPages - 4));
    public int LastPage => Math.Min(TotalPages, FirstPage + 4);
    public string? LoadError { get; private set; }
    public OpenForJobBannerViewModel? OpenForJobStatus { get; private set; }
    public bool HasSearchFilters => !string.IsNullOrWhiteSpace(Keyword)
        || !string.IsNullOrWhiteSpace(Province) || IndustryId.HasValue;

    public async Task<IActionResult> OnGetAsync()
    {
        Keyword = Keyword?.Trim();
        Province = Province?.Trim();
        PageNumber = Math.Clamp(PageNumber, 1, int.MaxValue / PageSize);

        if (User.IsInRole(Role.Candidate))
        {
            try
            {
                var status = await apiClient.Api.CandidateProfiles.OpenForJobStatus.GetAsync();
                if (status != null)
                {
                    OpenForJobStatus = new OpenForJobBannerViewModel
                    {
                        IsOpenForJob = status.IsOpenForJob ?? false,
                        CanRefresh = status.CanRefresh ?? false,
                        OpenForJobUntil = status.OpenForJobUntil,
                        ReturnUrl = Url.Page("/Public/Jobs", GetPageRouteValues(PageNumber))
                    };
                }
            }
            catch
            {
                //ignored
            }
        }

        try
        {
            Industries = (await apiClient.Api.Industries.GetAsync() ?? [])
                .OrderBy(industry => industry.Name).ToList();
            Provinces = (await apiClient.Api.Addresses.Provinces.GetAsync() ?? [])
                .Where(province => !string.IsNullOrWhiteSpace(province.Name))
                .Select(province => province.Name!.Trim())
                .Distinct().OrderBy(name => name).ToList();

            if (!ModelState.IsValid)
            {
                LoadError = "Bộ lọc hoặc số trang không hợp lệ. Vui lòng kiểm tra lại.";
                return Page();
            }

            var skip = (PageNumber - 1) * PageSize;

            var result = await apiClient.Api.Jobs
                .GetPagedWithODataAsync<JobResponseDto, PagedResponseDtoOfJobResponseDto>(q =>
                {
                    // API giữ thứ tự đẩy tin LastBumpedAt trước khi áp dụng phân trang.
                    q.Skip(skip).Top(PageSize);
                }, BuildSearchFilter());

            Jobs = result?.Items ?? [];
            TotalCount = result?.TotalCount ?? 0;

            if (PageNumber > TotalPages)
            {
                return RedirectToPage("/Public/Jobs", new { Keyword, Province, IndustryId, PageNumber = TotalPages });
            }
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 401)
        {
            throw;
        }
        catch (ApiException ex)
        {
            LoadError = ex.ToFriendlyErrorMessage();
        }
        catch (Exception)
        {
            LoadError = "Đã xảy ra lỗi khi tải danh sách việc làm. Vui lòng thử lại.";
        }

        return Page();
    }

    private string? BuildSearchFilter()
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var keyword = Keyword.ToLowerInvariant().Replace("'", "''");
            filters.Add($"(contains(tolower(Title),'{keyword}')"
                + $" or contains(tolower(EmployerCompanyName),'{keyword}')"
                + $" or contains(tolower(IndustryName),'{keyword}')"
                + $" or contains(tolower(Description),'{keyword}'))");
        }

        if (!string.IsNullOrWhiteSpace(Province))
        {
            var province = Province.ToLowerInvariant().Replace("'", "''");
            filters.Add($"tolower(ProvinceName) eq '{province}'");
        }

        if (IndustryId.HasValue)
        {
            filters.Add($"IndustryId eq {IndustryId.Value:D}");
        }

        // additionalFilter được nối vào URL; mã hóa để giữ nguyên &, +, # trong giá trị tìm kiếm.
        return filters.Count == 0 ? null : Uri.EscapeDataString(string.Join(" and ", filters));
    }

    public Dictionary<string, string> GetPageRouteValues(int pageNumber) => new()
    {
        [nameof(Keyword)] = Keyword ?? string.Empty,
        [nameof(Province)] = Province ?? string.Empty,
        [nameof(IndustryId)] = IndustryId?.ToString() ?? string.Empty,
        [nameof(PageNumber)] = pageNumber.ToString()
    };

    public static string FormatLocation(JobResponseDto job)
    {
        var parts = new[] { job.CommuneName, job.ProvinceName }
            .Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim());
        return string.Join(", ", parts) is { Length: > 0 } location ? location : "Toàn quốc";
    }

    public static string FormatSalariesSummary(List<JobSalaryResponseDto>? salaries) => salaries?.Count switch
    {
        null or 0 => "Thỏa thuận",
        1 => JobDetailModel.FormatSalary(salaries[0]),
        _ => $"{JobDetailModel.FormatSalary(salaries[0])} (+{salaries.Count - 1} mức lương)"
    };

    public async Task<IActionResult> OnPostToggleOpenForJobStatusAsync(bool enable, string? returnUrl = null)
    {
        if (!User.IsInRole(Role.Candidate))
        {
            return Forbid();
        }

        try
        {
            await apiClient.Api.CandidateProfiles.OpenForJobStatus.PutAsync(
                new ToggleOpenForJobStatusRequestDto { Enable = enable });

            if (enable)
            {
                TempData.SetSuccessMessage("Đã bật/làm mới trạng thái tìm việc thành công (hiệu lực trong 7 ngày).");
            }
            else
            {
                TempData.SetSuccessMessage("Đã tắt trạng thái tìm việc thành công.");
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi cập nhật trạng thái tìm việc.");
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Public/Jobs");
    }
}
