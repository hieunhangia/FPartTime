using Microsoft.AspNetCore.Identity;

namespace Repository;

public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
        { Code = nameof(DefaultError), Description = "Đã xảy ra lỗi chưa xác định." };

    public override IdentityError ConcurrencyFailure() => new()
        { Code = nameof(ConcurrencyFailure), Description = "Lỗi đồng thời, đối tượng đã bị sửa đổi." };

    public override IdentityError PasswordMismatch() => new()
        { Code = nameof(PasswordMismatch), Description = "Mật khẩu không chính xác." };

    public override IdentityError InvalidToken() => new()
        { Code = nameof(InvalidToken), Description = "Mã xác nhận không hợp lệ." };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
        { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Đổi mã phục hồi thất bại." };

    public override IdentityError LoginAlreadyAssociated() => new()
        { Code = nameof(LoginAlreadyAssociated), Description = "Người dùng đã có tài khoản đăng nhập này." };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"Tên đăng nhập '{userName}' không hợp lệ, chỉ có thể chứa chữ cái và số."
    };

    public override IdentityError InvalidEmail(string? email) => new()
        { Code = nameof(InvalidEmail), Description = $"Email '{email}' không hợp lệ." };

    public override IdentityError DuplicateUserName(string userName) => new()
        { Code = nameof(DuplicateUserName), Description = $"Tên đăng nhập '{userName}' đã được sử dụng." };

    public override IdentityError DuplicateEmail(string email) => new()
        { Code = nameof(DuplicateEmail), Description = $"Email '{email}' đã được sử dụng." };

    public override IdentityError InvalidRoleName(string? role) => new()
        { Code = nameof(InvalidRoleName), Description = $"Tên quyền '{role}' không hợp lệ." };

    public override IdentityError DuplicateRoleName(string role) => new()
        { Code = nameof(DuplicateRoleName), Description = $"Tên quyền '{role}' đã được sử dụng." };

    public override IdentityError UserAlreadyHasPassword() => new()
        { Code = nameof(UserAlreadyHasPassword), Description = "Người dùng đã có mật khẩu." };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = "Chức năng khóa tài khoản không được kích hoạt cho người dùng này."
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
        { Code = nameof(UserAlreadyInRole), Description = $"Người dùng đã có quyền '{role}'." };

    public override IdentityError UserNotInRole(string role) => new()
        { Code = nameof(UserNotInRole), Description = $"Người dùng không có quyền '{role}'." };

    public override IdentityError PasswordTooShort(int length) => new()
        { Code = nameof(PasswordTooShort), Description = $"Mật khẩu phải có ít nhất {length} ký tự." };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (không phải chữ và số)."
    };

    public override IdentityError PasswordRequiresDigit() => new()
        { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải chứa ít nhất 1 chữ số ('0'-'9')." };

    public override IdentityError PasswordRequiresLower() => new()
        { Code = nameof(PasswordRequiresLower), Description = "Mật khẩu phải chứa ít nhất 1 chữ thường ('a'-'z')." };

    public override IdentityError PasswordRequiresUpper() => new()
        { Code = nameof(PasswordRequiresUpper), Description = "Mật khẩu phải chứa ít nhất 1 chữ hoa ('A'-'Z')." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = $"Mật khẩu phải sử dụng ít nhất {uniqueChars} ký tự khác nhau."
    };
}