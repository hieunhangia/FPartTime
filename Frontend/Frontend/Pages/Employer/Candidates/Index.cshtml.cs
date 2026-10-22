using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.Candidates;

[Authorize(Roles = Role.Employer)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Gender? GenderFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinAge { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxAge { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ProvinceCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CommuneCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public List<CandidateSummaryResponseDto> Candidates { get; private set; } = [];

    public long TotalCount { get; private set; }

    public int TotalPages { get; private set; } = 1;

    public List<ProvinceDto> Provinces { get; private set; } = [];

    public List<CommuneDto> Communes { get; private set; } = [];

    public bool HasPermissionError { get; private set; }

    public string? PermissionErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        // 1. Tải danh mục Tỉnh / Thành phố
        try
        {
            Provinces = await apiClient.Api.Addresses.Provinces.GetAsync() ?? [];
        }
        catch
        {
            Provinces = [];
        }

        // 2. Nếu đã chọn Tỉnh / Thành phố, tải danh mục Xã / Phường tương ứng
        if (!string.IsNullOrWhiteSpace(ProvinceCode))
        {
            try
            {
                Communes = await apiClient.Api.Addresses.Provinces[ProvinceCode].Communes.GetAsync() ?? [];
            }
            catch
            {
                Communes = [];
            }
        }

        // 3. Xây dựng điều kiện lọc DateOfBirth theo độ tuổi
        string? dateFilter = null;
        if (MinAge.HasValue || MaxAge.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dateClauses = new List<string>();
            if (MinAge.HasValue)
            {
                var maxDobDate = today.AddYears(-MinAge.Value);
                dateClauses.Add($"DateOfBirth le {maxDobDate:yyyy-MM-dd}");
            }

            if (MaxAge.HasValue)
            {
                var minDobDate = today.AddYears(-MaxAge.Value - 1).AddDays(1);
                dateClauses.Add($"DateOfBirth ge {minDobDate:yyyy-MM-dd}");
            }

            if (dateClauses.Count > 0)
            {
                dateFilter = string.Join(" and ", dateClauses);
            }
        }

        // 4. Thực hiện truy vấn danh sách ứng viên qua OData
        var skip = (PageNumber - 1) * PageSize;

        try
        {
            var result = await apiClient.Api.Employer.Candidates.Search
                .GetPagedWithODataAsync<CandidateSummaryResponseDto, PagedResponseDtoOfCandidateSummaryResponseDto>(q =>
                {
                    if (GenderFilter.HasValue)
                    {
                        q.Filter(c => c.Gender == GenderFilter.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(CommuneCode))
                    {
                        q.Filter(c => c.PreferredWorkCommuneCode == CommuneCode);
                    }
                    else if (!string.IsNullOrWhiteSpace(ProvinceCode))
                    {
                        q.Filter(c => c.PreferredWorkProvinceCode == ProvinceCode);
                    }

                    q.Skip(skip).Top(PageSize);
                }, dateFilter);

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
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tìm kiếm ứng viên.");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetCommunesAsync(string provinceCode)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return new JsonResult(new List<object>());
        }

        try
        {
            var communes = await apiClient.Api.Addresses.Provinces[provinceCode].Communes.GetAsync();
            return new JsonResult(communes?.Select(c => new { code = c.Code, name = c.Name }) ?? []);
        }
        catch
        {
            return new JsonResult(new List<object>());
        }
    }

    public async Task<IActionResult> OnGetCandidateDetailModalAsync(Guid id)
    {
        try
        {
            var detail = await apiClient.Api.Employer.Candidates[id].GetAsync();
            if (detail == null)
            {
                return NotFound();
            }

            var vm = CandidateDetailModalViewModel.FromDetailDto(detail);
            return Partial("_CandidateDetailModalContent", vm);
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.ResponseStatusCode, ex.ToFriendlyErrorMessage());
        }
        catch
        {
            return StatusCode(500, "Đã xảy ra lỗi khi tải thông tin hồ sơ ứng viên.");
        }
    }

    public string GetCandidateGenderDisplay(Gender? gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Khác"
    };

    public int? CalculateAge(Date? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dob = new DateOnly(dateOfBirth.Value.Year, dateOfBirth.Value.Month, dateOfBirth.Value.Day);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
