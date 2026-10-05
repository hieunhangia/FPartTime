using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Identity;

public class PasswordLoginModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public LoginFormModel LoginForm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return RedirectToPage("LoginOrRegister");
        }

        try
        {
            var response = await apiClient.Api.Identity.AccountExists.PostAsync(new AccountExistRequestDto
            {
                PhoneNumber = phoneNumber
            });

            if (response?.Exists != true)
            {
                return RedirectToPage("Register", new { phoneNumber });
            }
        }
        catch
        {
            return RedirectToPage("LoginOrRegister");
        }

        LoginForm.PhoneNumber = phoneNumber;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var tokenResponse = await apiClient.Api.Identity.PasswordLogin.PostAsync(new PasswordLoginRequestDto
            {
                PhoneNumber = LoginForm.PhoneNumber,
                Password = LoginForm.Password
            });
            if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null)
            {
                TempData.SetErrorMessage("Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin đăng nhập.");
                return Page();
            }

            await HttpContext.SignInWithApiTokenAsync(tokenResponse.AccessToken, tokenResponse.RefreshToken);
            TempData.SetSuccessMessage("Đăng nhập thành công.");
            return Redirect("/");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi trong quá trình đăng nhập.");
            return Page();
        }
    }

    public class LoginFormModel
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }
}