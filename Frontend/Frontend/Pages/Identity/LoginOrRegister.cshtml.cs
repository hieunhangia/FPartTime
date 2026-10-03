using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Identity;

public class LoginOrRegisterModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public PhoneFormModel PhoneForm { get; set; } = new();

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

            return RedirectToPage(response?.Exists == true ? "PasswordLogin" : "Register", new
            {
                phoneNumber = PhoneForm.PhoneNumber
            });
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi. Vui lòng thử lại sau.");
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