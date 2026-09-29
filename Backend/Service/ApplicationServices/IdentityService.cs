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

    private class OtpCacheItem(string otp)
    {
        public string Otp { get; } = otp;
        private int _failedAttempts;
        public int IncrementFailedAttempts() => Interlocked.Increment(ref _failedAttempts);
    }

    public async Task<AccountExistResponseDto> AccountExistsAsync(AccountExistRequestDto dto) =>
        new() { Exists = await userManager.FindByNameAsync(dto.PhoneNumber) != null };

    public async Task RequestOtpForRegisterAsync(RequestOtpForRegisterRequestDto dto)
    {
        if (await userManager.FindByNameAsync(dto.PhoneNumber) != null)
        {
            throw new ConflictException("Số điện thoại đã đăng ký tài khoản từ trước.");
        }

        var otp = RandomNumberGenerator.GetInt32(1000000).ToString("D6");
        cache.Set($"{OtpForRegisterCachePrefix}{dto.PhoneNumber}", new OtpCacheItem(otp), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(BusinessRuleConstants.Identity.OtpExpiresInMinutes)
        });
        await smsSenderService.SendSmsAsync(dto.PhoneNumber,
            $"FPartTime - Mã OTP của bạn là: {otp}. Mã này sẽ hết hạn sau {BusinessRuleConstants.Identity.OtpExpiresInMinutes} phút.");
    }

    public async Task RegisterAsync(RegisterRequestDto dto)
    {
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

        if (await userManager.FindByNameAsync(dto.PhoneNumber) != null)
        {
            throw new ConflictException("Số điện thoại đã đăng ký tài khoản từ trước.");
        }

        if (!(await userManager.CreateAsync(new User { UserName = dto.PhoneNumber }, dto.Password)).Succeeded)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi tạo tài khoản người dùng mới.");
        }

        cache.Remove(cacheKey);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto)
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

public class RequestOtpForRegisterRequestDto
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

public class LoginRequestDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.Identity.PasswordRegex,
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; set; } = string.Empty;
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