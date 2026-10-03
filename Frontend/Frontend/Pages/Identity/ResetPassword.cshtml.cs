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

public class ResetPasswordModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public ResetPasswordFormModel ResetPasswordForm { get; set; } = new();

    public async Task<IActionResult> OnGet(string? phoneNumber)
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

        ResetPasswordForm.PhoneNumber = phoneNumber;
        return Page();
    }

    public async Task<IActionResult> OnPostSendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(ResetPasswordForm.PhoneNumber))
        {
            return new JsonResult(new { success = false, message = "Số điện thoại không hợp lệ." });
        }

        try
        {
            await apiClient.Api.Identity.RequestOtpForResetPassword.PostAsync(new RequestOtpRequestDto
            {
                PhoneNumber = ResetPasswordForm.PhoneNumber
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
            await apiClient.Api.Identity.ResetPassword.PostAsync(new ResetPasswordRequestDto
            {
                PhoneNumber = ResetPasswordForm.PhoneNumber,
                NewPassword = ResetPasswordForm.NewPassword,
                Otp = ResetPasswordForm.Otp
            });

            TempData.SetSuccessMessage("Đổi mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.");
            return RedirectToPage("PasswordLogin", new { phoneNumber = ResetPasswordForm.PhoneNumber });
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi trong quá trình đặt lại mật khẩu.");
            return Page();
        }
    }

    public class ResetPasswordFormModel
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
        [Display(Name = "Mã OTP")]
        public string Otp { get; set; } = string.Empty;
    }
}