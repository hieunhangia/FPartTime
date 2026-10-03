using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Repository;
using Repository.Models.Users;
using Service.ExternalServices;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices;

public class IdentityService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    SmsSenderService smsSenderService,
    IConfiguration configuration,
    IMemoryCache cache,
    JsonWebTokenHandler jsonWebTokenHandler)
{
    private const string OtpForRegisterCachePrefix = "Otp_For_Register_";
    private const string OtpForLoginCachePrefix = "Otp_For_Login_";
    private const string OtpForResetPasswordCachePrefix = "Otp_For_Reset_Password_";

    private class OtpCacheItem(string otp)
    {
        public string Otp { get; } = otp;
        private int _failedAttempts;
        public int IncrementFailedAttempts() => Interlocked.Increment(ref _failedAttempts);
    }

    private enum RequestOtpPurpose
    {
        Register,
        Login,
        ResetPassword
    }

    public async Task<AccountExistResponseDto> AccountExistsAsync(AccountExistRequestDto dto) =>
        new() { Exists = await userManager.FindByNameAsync(dto.PhoneNumber) != null };

    public async Task RequestOtpForRegisterAsync(RequestOtpRequestDto dto) =>
        await RequestOtpAsync(dto.PhoneNumber, RequestOtpPurpose.Register);

    public async Task RegisterAsync(RegisterRequestDto dto)
    {
        if (await userManager.FindByNameAsync(dto.PhoneNumber) != null)
        {
            throw new ConflictException("Số điện thoại đã đăng ký tài khoản từ trước.");
        }

        var cacheKey = $"{OtpForRegisterCachePrefix}{dto.PhoneNumber}";
        if (!cache.TryGetValue<OtpCacheItem>(cacheKey, out var cacheItem) || cacheItem == null)
        {
            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (cacheItem.Otp != dto.Otp)
        {
            var attempts = cacheItem.IncrementFailedAttempts();
            if (attempts >= BusinessRuleConstants.Identity.MaxOtpFailedAttempts)
            {
                cache.Remove(cacheKey);
            }

            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (!(await userManager.CreateAsync(new User { UserName = dto.PhoneNumber }, dto.Password)).Succeeded)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi tạo tài khoản người dùng mới.");
        }

        cache.Remove(cacheKey);
    }

    public async Task<TokenResponseDto> PasswordLoginAsync(PasswordLoginRequestDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.PhoneNumber);
        if (user == null)
        {
            throw new UnauthorizedException("Số điện thoại hoặc mật khẩu không đúng.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Tài khoản đã bị khóa.");
        }

        if (!await userManager.CheckPasswordAsync(user, dto.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedException("Số điện thoại hoặc mật khẩu không đúng.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await GenerateTokensAsync(user);
    }

    public async Task RequestOtpForLoginAsync(RequestOtpRequestDto dto) =>
        await RequestOtpAsync(dto.PhoneNumber, RequestOtpPurpose.Login);

    public async Task<TokenResponseDto> OtpLoginAsync(OtpLoginRequestDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.PhoneNumber);
        if (user == null)
        {
            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Tài khoản đã bị khóa.");
        }

        var cacheKey = $"{OtpForLoginCachePrefix}{dto.PhoneNumber}";
        if (!cache.TryGetValue<OtpCacheItem>(cacheKey, out var cacheItem) || cacheItem == null)
        {
            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (cacheItem.Otp != dto.Otp)
        {
            var attempts = cacheItem.IncrementFailedAttempts();
            if (attempts >= BusinessRuleConstants.Identity.MaxOtpFailedAttempts)
            {
                cache.Remove(cacheKey);
            }

            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        cache.Remove(cacheKey);
        await userManager.ResetAccessFailedCountAsync(user);
        return await GenerateTokensAsync(user);
    }

    public async Task RequestOtpForResetPasswordAsync(RequestOtpRequestDto dto) =>
        await RequestOtpAsync(dto.PhoneNumber, RequestOtpPurpose.ResetPassword);

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.PhoneNumber);
        if (user == null)
        {
            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Tài khoản đã bị khóa.");
        }

        var cacheKey = $"{OtpForResetPasswordCachePrefix}{dto.PhoneNumber}";
        if (!cache.TryGetValue<OtpCacheItem>(cacheKey, out var cacheItem) || cacheItem == null)
        {
            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        if (cacheItem.Otp != dto.Otp)
        {
            var attempts = cacheItem.IncrementFailedAttempts();
            if (attempts >= BusinessRuleConstants.Identity.MaxOtpFailedAttempts)
            {
                cache.Remove(cacheKey);
            }

            throw new BadRequestException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        var result = await userManager.ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user),
            dto.NewPassword);
        if (!result.Succeeded)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi đặt lại mật khẩu.");
        }

        cache.Remove(cacheKey);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequestDto dto)
    {
        var authenticatedUser = await userManager.GetUserAsync(user);
        if (authenticatedUser?.UserName == null)
        {
            throw new UnauthorizedException("Người dùng chưa đăng nhập hoặc không hợp lệ.");
        }

        if (await userManager.IsLockedOutAsync(authenticatedUser))
        {
            throw new UnauthorizedException("Tài khoản đã bị khóa.");
        }

        var result = await userManager.ChangePasswordAsync(authenticatedUser, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi thay đổi mật khẩu.");
        }
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var storedRefreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == dto.RefreshToken);
        if (storedRefreshToken == null || storedRefreshToken.ExpiryDate < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (storedRefreshToken.IsUsed || storedRefreshToken.IsRevoked)
        {
            var activeTokens = await dbContext.RefreshTokens
                .Where(x => x.UserId == storedRefreshToken.UserId && !x.IsRevoked)
                .ToListAsync();
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }

            await dbContext.SaveChangesAsync();
            throw new UnauthorizedException("Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
        }

        storedRefreshToken.IsUsed = true;
        storedRefreshToken.UsedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(storedRefreshToken.UserId);
        if (user == null || await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Tài khoản người dùng không tồn tại hoặc đã bị khóa.");
        }

        return await GenerateTokensAsync(user);
    }

    public async Task LogoutAsync(LogoutRequestDto dto)
    {
        var storedRefreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == dto.RefreshToken);
        if (storedRefreshToken == null || storedRefreshToken.IsRevoked)
        {
            return;
        }

        storedRefreshToken.IsRevoked = true;
        await dbContext.SaveChangesAsync();
    }

    private async Task RequestOtpAsync(string phoneNumber, RequestOtpPurpose purpose)
    {
        var user = await userManager.FindByNameAsync(phoneNumber);
        string cacheKeyPrefix;
        switch (purpose)
        {
            case RequestOtpPurpose.Register:
                if (user != null)
                {
                    throw new ConflictException("Số điện thoại đã đăng ký tài khoản từ trước.");
                }

                cacheKeyPrefix = OtpForRegisterCachePrefix;

                break;
            case RequestOtpPurpose.Login:
                if (user == null)
                {
                    throw new BadRequestException("Số điện thoại chưa được đăng ký tài khoản.");
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    throw new UnauthorizedException("Tài khoản người dùng đã bị khóa.");
                }

                cacheKeyPrefix = OtpForLoginCachePrefix;
                break;
            case RequestOtpPurpose.ResetPassword:
                if (user == null)
                {
                    throw new BadRequestException("Số điện thoại chưa được đăng ký tài khoản.");
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    throw new UnauthorizedException("Tài khoản người dùng đã bị khóa.");
                }

                cacheKeyPrefix = OtpForResetPasswordCachePrefix;
                break;
            default:
                throw new BadRequestException("Mục đích yêu cầu OTP không hợp lệ.");
        }

        var otp = RandomNumberGenerator.GetInt32(1000000).ToString("D6");
        cache.Set($"{cacheKeyPrefix}{phoneNumber}", new OtpCacheItem(otp), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(BusinessRuleConstants.Identity.OtpExpiresInMinutes)
        });
        await smsSenderService.SendAsync(phoneNumber,
            $"FPartTime - Mã OTP của bạn là: {otp}. Mã này sẽ hết hạn sau {BusinessRuleConstants.Identity.OtpExpiresInMinutes} phút.");
    }

    private async Task<TokenResponseDto> GenerateTokensAsync(User user)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id) };
        claims.AddRange((await userManager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
        var jwtSettings = configuration.GetSection("JwtSettings");
        var accessToken = jsonWebTokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.GetValue<double>("AccessTokenExpirationInMinutes")),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
                    SecurityAlgorithms.HmacSha256)
        });
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(jwtSettings.GetValue<double>("RefreshTokenExpirationInDays")),
            IsUsed = false,
            IsRevoked = false
        });
        await dbContext.SaveChangesAsync();
        return new TokenResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
    }
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

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh Token là bắt buộc.")]
    public required string RefreshToken { get; set; }
}

public class TokenResponseDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public class LogoutRequestDto
{
    [Required(ErrorMessage = "Refresh Token là bắt buộc.")]
    public required string RefreshToken { get; set; }
}