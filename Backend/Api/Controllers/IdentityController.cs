using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.ApplicationServices;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController(IdentityService identityService) : ControllerBase
{
    [HttpPost("account-exists")]
    [ProducesResponseType<AccountExistResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AccountExists([FromBody] AccountExistRequestDto dto) =>
        Ok(await identityService.AccountExistsAsync(dto));

    [HttpPost("request-otp-for-register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task RequestOtpForRegister([FromBody] RequestOtpRequestDto dto) =>
        await identityService.RequestOtpForRegisterAsync(dto);

    [HttpPost("register")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto) =>
        Ok(await identityService.RegisterAsync(dto));

    [HttpPost("password-login")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PasswordLogin([FromBody] PasswordLoginRequestDto dto) =>
        Ok(await identityService.PasswordLoginAsync(dto));

    [HttpPost("request-otp-for-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task RequestOtpForLogin([FromBody] RequestOtpRequestDto dto) =>
        await identityService.RequestOtpForLoginAsync(dto);

    [HttpPost("otp-login")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OtpLogin([FromBody] OtpLoginRequestDto dto) =>
        Ok(await identityService.OtpLoginAsync(dto));

    [HttpPost("request-otp-for-reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task RequestOtpForResetPassword([FromBody] RequestOtpRequestDto dto) =>
        await identityService.RequestOtpForResetPasswordAsync(dto);

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task ResetPassword([FromBody] ResetPasswordRequestDto dto) =>
        await identityService.ResetPasswordAsync(dto);

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task ChangePassword([FromBody] ChangePasswordRequestDto dto) =>
        await identityService.ChangePasswordAsync(User, dto);

    [HttpPost("refresh-token")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto) =>
        Ok(await identityService.RefreshTokenAsync(dto));

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task Logout([FromBody] LogoutRequestDto dto) => await identityService.LogoutAsync(dto);
}