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
public class DetailModel(ApiClient apiClient) : PageModel
{
    public UserDetailResponseDto? Account { get; private set; }

    [BindProperty]
    public List<string> SelectedRoles { get; set; } = [];

    [BindProperty]
    [Range(1, 8760, ErrorMessage = "Thời gian khóa phải từ 1 đến 8760 giờ.")]
    public int LockDurationHours { get; set; } = 24;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
        await LoadPageAsync(id, populateRoles: true, cancellationToken);

    public async Task<IActionResult> OnPostRolesAsync(Guid id, CancellationToken cancellationToken)
    {
        var allowedRoles = new[] { Role.Censor, Role.Candidate };
        if (SelectedRoles.Any(role => !allowedRoles.Contains(role)) ||
                 SelectedRoles.Distinct().Count() != SelectedRoles.Count)
            ModelState.AddModelError(nameof(SelectedRoles), "Danh sách quyền không hợp lệ.");

        if (!ModelState.IsValid)
            return await LoadPageAsync(id, populateRoles: false, cancellationToken);

        try
        {
            await apiClient.Api.Admin.Users[id].Roles.PutAsync(new UpdateUserRolesRequestDto
            {
                Roles = SelectedRoles
            }, cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã cập nhật quyền tài khoản.");
            return RedirectToPage(new { id });
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
            return await LoadPageAsync(id, populateRoles: false, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ tài khoản. Vui lòng thử lại sau.");
            return await LoadPageAsync(id, populateRoles: false, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await LoadPageAsync(id, populateRoles: true, cancellationToken);

        try
        {
            await apiClient.Api.Admin.Users[id].Lockout.PutAsync(new LockUserRequestDto
            {
                LockoutEndUtc = new DateTimeOffset(DateTime.UtcNow).AddHours(LockDurationHours)
            }, cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã khóa tài khoản theo thời gian đã chọn.");
            return RedirectToPage(new { id });
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
            return await LoadPageAsync(id, populateRoles: true, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ tài khoản. Vui lòng thử lại sau.");
            return await LoadPageAsync(id, populateRoles: true, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostUnlockAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.Api.Admin.Users[id].Lockout.DeleteAsync(cancellationToken: cancellationToken);
            TempData.SetSuccessMessage("Đã mở khóa tài khoản.");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            TempData.SetErrorMessage("Không thể kết nối đến dịch vụ tài khoản. Vui lòng thử lại sau.");
        }

        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadPageAsync(Guid id, bool populateRoles,
        CancellationToken cancellationToken)
    {
        try
        {
            Account = await apiClient.Api.Admin.Users[id].GetAsync(cancellationToken: cancellationToken);
            if (Account is null)
            {
                TempData.SetErrorMessage("Không tìm thấy tài khoản.");
                return RedirectToPage("./Index");
            }

            if (populateRoles)
                SelectedRoles = (Account.Roles ?? []).Where(role => role is Role.Censor or Role.Candidate).ToList();

            return Page();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("./Index");
        }
        catch (HttpRequestException)
        {
            TempData.SetErrorMessage("Không thể kết nối đến dịch vụ tài khoản. Vui lòng thử lại sau.");
            return RedirectToPage("./Index");
        }
    }
}
