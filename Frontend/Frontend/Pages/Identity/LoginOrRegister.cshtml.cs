using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Identity;

public class LoginOrRegisterModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public PhoneFormModel PhoneForm { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await apiClient.Api.Identity.AccountExists.PostAsync(new AccountExistRequestDto
            {
                PhoneNumber = PhoneForm.PhoneNumber
            });

            return RedirectToPage(response?.Exists == true ? "Login" : "Register", new
            {
                phoneNumber = PhoneForm.PhoneNumber
            });
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ToFriendlyErrorMessage();
            return Page();
        }
        catch
        {
            ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau.";
            return Page();
        }
    }

    public class PhoneFormModel
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}