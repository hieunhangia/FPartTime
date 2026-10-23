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
    public List<string> Provinces { get; private set; } = [];
    public List<IndustrySummary> Industries { get; private set; } = [];
    public List<IndustrySummary> PopularIndustries { get; private set; } = [];
    public List<JobResponseDto> PopularJobs { get; private set; } = [];
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

        try
        {
            var response = await apiClient.Api.Jobs.GetAsync();
            var allJobs = response?.Items ?? [];

            // Giữ thứ tự ưu tiên đẩy tin do API trả về.
            PopularJobs = allJobs.Take(6).ToList();

            Provinces = allJobs
                .Where(j => !string.IsNullOrWhiteSpace(j.ProvinceName))
                .Select(j => j.ProvinceName!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            Industries = allJobs
                .Where(j => j.IndustryId.HasValue && !string.IsNullOrWhiteSpace(j.IndustryName))
                .GroupBy(j => j.IndustryId!.Value)
                .Select(group => new IndustrySummary(group.Key, group.First().IndustryName!.Trim(), group.Count()))
                .OrderBy(industry => industry.Name)
                .ToList();

            PopularIndustries = Industries
                .OrderByDescending(industry => industry.JobCount)
                .ThenBy(industry => industry.Name)
                .Take(5)
                .ToList();
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

    public sealed record IndustrySummary(Guid Id, string Name, int JobCount);
}
