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

public class OtpLoginModel(ApiClient apiClient) : PageModel
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
            var tokenResponse = await apiClient.Api.Identity.OtpLogin.PostAsync(new OtpLoginRequestDto
            {
                PhoneNumber = LoginForm.PhoneNumber,
                Otp = LoginForm.Otp
            });

            if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null)
            {
                TempData.SetErrorMessage("Đăng nhập thất bại. Vui lòng kiểm tra lại mã OTP.");
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
            TempData.SetErrorMessage("Đã xảy ra lỗi trong quá trình đăng nhập bằng OTP.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginForm.PhoneNumber))
        {
            return new JsonResult(new { success = false, message = "Số điện thoại không hợp lệ." });
        }

        try
        {
            await apiClient.Api.Identity.RequestOtpForLogin.PostAsync(new RequestOtpRequestDto
            {
                PhoneNumber = LoginForm.PhoneNumber
            });

            return new JsonResult(new { success = true, message = "Đã gửi mã OTP đến số điện thoại của bạn." });
        }
        catch (ApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.ToFriendlyErrorMessage() });
        }
        catch
        {
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi khi gửi mã OTP." });
        }
    }

    public class LoginFormModel
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
        [Display(Name = "Mã OTP")]
        public string Otp { get; set; } = string.Empty;
    }
}
