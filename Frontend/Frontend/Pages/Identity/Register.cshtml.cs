using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Identity;

public class RegisterModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public RegisterFormModel RegisterForm { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

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
                return RedirectToPage("Login", new { phoneNumber });
            }
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

        RegisterForm.PhoneNumber = phoneNumber;
        return Page();
    }

    public async Task<IActionResult> OnPostSendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterForm.PhoneNumber))
        {
            ErrorMessage = "Số điện thoại không hợp lệ.";
            return Page();
        }

        try
        {
            await apiClient.Api.Identity.RequestOtpForRegister.PostAsync(new RequestOtpForRegisterRequestDto
            {
                PhoneNumber = RegisterForm.PhoneNumber
            });

            SuccessMessage = "Đã gửi mã OTP đến số điện thoại của bạn.";
            return Page();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ToFriendlyErrorMessage();
            return Page();
        }
        catch
        {
            ErrorMessage = "Đã xảy ra lỗi khi gửi mã OTP.";
            return Page();
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
            await apiClient.Api.Identity.Register.PostAsync(new RegisterRequestDto
            {
                PhoneNumber = RegisterForm.PhoneNumber,
                Password = RegisterForm.Password,
                Otp = RegisterForm.Otp
            });

            try
            {
                var loginResponse = await apiClient.Api.Identity.Login.PostAsync(new LoginRequestDto
                {
                    PhoneNumber = RegisterForm.PhoneNumber,
                    Password = RegisterForm.Password
                });

                if (loginResponse?.AccessToken is not null && loginResponse.RefreshToken is not null)
                {
                    AuthCookieHelper.SetAuthCookies(Response, loginResponse);
                    return Redirect("/");
                }
            }
            catch
            {
                // ignored
            }

            return RedirectToPage("Login", new { phoneNumber = RegisterForm.PhoneNumber });
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ToFriendlyErrorMessage();
            return Page();
        }
        catch
        {
            ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng ký tài khoản.";
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