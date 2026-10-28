using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Manager.Employers;

[Authorize(Roles = Role.Manager)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự.")]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageNumber { get; set; } = 1;

    public List<ManagedEmployerSummaryResponseDto> Employers { get; private set; } = [];
    public long TotalCount { get; private set; }
    public int TotalPages { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await apiClient.Api.Manager.Employers.GetAsync(request =>
            {
                request.QueryParameters.SearchTerm = SearchTerm?.Trim();
                request.QueryParameters.PageNumber = PageNumber;
                request.QueryParameters.PageSize = PageSize;
            }, cancellationToken);

            Employers = result?.Items ?? [];
            TotalCount = result?.TotalCount ?? 0;
            TotalPages = (int)Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
            if (PageNumber > TotalPages)
                return RedirectToPage(new { SearchTerm, PageNumber = TotalPages });
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ nhà tuyển dụng. Vui lòng thử lại sau.");
        }

        return Page();
    }
}
