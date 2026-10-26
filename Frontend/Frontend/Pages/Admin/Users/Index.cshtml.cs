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

namespace Frontend.Pages.Admin.Users;

[Authorize(Roles = Role.Admin)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^(Censor|Employer|Candidate)$", ErrorMessage = "Quyền lọc không hợp lệ.")]
    public string? RoleFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    [StringLength(10, ErrorMessage = "Số điện thoại tìm kiếm không được vượt quá 10 ký tự.")]
    [RegularExpression(@"^\d{1,10}$", ErrorMessage = "Số điện thoại tìm kiếm chỉ gồm chữ số.")]
    public string? PhoneNumberPrefix { get; set; }

    [BindProperty(SupportsGet = true)]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageNumber { get; set; } = 1;

    public List<UserDetailResponseDto> Accounts { get; private set; } = [];
    public long TotalCount { get; private set; }
    public int TotalPages { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await apiClient.Api.Admin.Users.GetAsync(request =>
            {
                request.QueryParameters.PageNumber = PageNumber;
                request.QueryParameters.PageSize = PageSize;
                request.QueryParameters.Role = RoleFilter;
                request.QueryParameters.PhoneNumberPrefix = PhoneNumberPrefix?.Trim();
            }, cancellationToken);

            Accounts = result?.Items ?? [];
            TotalCount = result?.TotalCount ?? 0;
            TotalPages = (int)Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
            if (PageNumber > TotalPages)
                return RedirectToPage(new { RoleFilter, PhoneNumberPrefix, PageNumber = TotalPages });
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            TempData.SetErrorMessage("Không thể kết nối đến dịch vụ tài khoản. Vui lòng thử lại sau.");
        }

        return Page();
    }
}
