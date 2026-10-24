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

public class IndexModel(ApiClient apiClient) : PageModel
{
    public List<ProvinceDto> Provinces { get; private set; } = [];
    public List<IndustryResponseDto> Industries { get; private set; } = [];
    public List<PopularIndustryResponseDto> PopularIndustries { get; private set; } = [];
    public List<JobResponseDto> PopularJobs { get; private set; } = [];
    public OpenForJobBannerViewModel? OpenForJobStatus { get; private set; }
    public bool HasSearchFilters => !string.IsNullOrWhiteSpace(Keyword)
        || !string.IsNullOrWhiteSpace(Province)
        || IndustryId.HasValue;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Province { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? IndustryId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (HasSearchFilters)
        {
            return RedirectToPage("/Public/Jobs", new { Keyword, Province, IndustryId });
        }

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
                        ReturnUrl = Url.Page("/Public/Index")
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
            var response = await apiClient.Api.Jobs
                .GetPagedWithODataAsync<JobResponseDto, PagedResponseDtoOfJobResponseDto>(q => q.Top(6));

            PopularJobs = response?.Items ?? [];

            Provinces = await apiClient.Api.Addresses.Provinces.GetAsync() ?? [];

            Industries = await apiClient.Api.Industries.GetAsync() ?? [];

            PopularIndustries = await apiClient.Api.Industries.Popular.GetAsync(
                config => config.QueryParameters.Limit = 5) ?? [];
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 401)
        {
            throw;
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

        return RedirectToPage("/Public/Index");
    }
}
