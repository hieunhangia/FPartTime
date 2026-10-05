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

public class RegisterModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public RegisterFormModel RegisterForm { get; set; } = new();

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

            if (response?.Exists == true)
            {
                return RedirectToPage("PasswordLogin", new { phoneNumber });
            }
        }
        catch
        {
            return RedirectToPage("LoginOrRegister");
        }

        RegisterForm.PhoneNumber = phoneNumber;
        return Page();
    }

    public async Task<IActionResult> OnPostSendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterForm.PhoneNumber))
        {
            return new JsonResult(new { success = false, message = "Số điện thoại không hợp lệ." });
        }

        try
        {
            await apiClient.Api.Identity.RequestOtpForRegister.PostAsync(new RequestOtpRequestDto
            {
                PhoneNumber = RegisterForm.PhoneNumber
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await apiClient.Api.Identity.Register.PostAsync(new RegisterRequestDto
            {
                PhoneNumber = RegisterForm.PhoneNumber,
                Password = RegisterForm.Password,
                Otp = RegisterForm.Otp
            });
            if (response?.AccessToken is null || response.RefreshToken is null)
            {
                TempData.SetErrorMessage("Đăng ký thất bại. Vui lòng kiểm tra lại thông tin đăng ký.");
                return Page();
            }

            await HttpContext.SignInWithApiTokenAsync(response.AccessToken, response.RefreshToken);
            TempData.SetSuccessMessage("Đăng ký tài khoản thành công.");
            return Redirect("/");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi trong quá trình đăng ký tài khoản.");
            return Page();
        }
    }

    public class RegisterFormModel
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

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
        [Display(Name = "Mã OTP")]
        public string Otp { get; set; } = string.Empty;
    }
}