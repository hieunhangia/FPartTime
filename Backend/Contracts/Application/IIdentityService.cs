using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Shared.Constants;

namespace Contracts.Application;

public interface IIdentityService
{
    Task<AccountExistResponseDto> AccountExistsAsync(AccountExistRequestDto dto);
    Task RequestOtpForRegisterAsync(RequestOtpRequestDto dto);
    Task<TokenResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<TokenResponseDto> PasswordLoginAsync(PasswordLoginRequestDto dto);
    Task RequestOtpForLoginAsync(RequestOtpRequestDto dto);
    Task<TokenResponseDto> OtpLoginAsync(OtpLoginRequestDto dto);
    Task RequestOtpForResetPasswordAsync(RequestOtpRequestDto dto);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto);
    Task ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequestDto dto);
}

public class AccountExistRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class AccountExistResponseDto
{
    public required bool Exists { get; set; }
}

public class RequestOtpRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
    public string Otp { get; set; } = string.Empty;
}

public class PasswordLoginRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; set; } = string.Empty;
}

public class OtpLoginRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
    public string Otp { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.OtpRegex, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu cũ phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public required string AccessToken { get; set; }
}