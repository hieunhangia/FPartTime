using Microsoft.AspNetCore.Mvc;
using Service.ApplicationServices;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IdentityController(IdentityService identityService) : ControllerBase
{
    [HttpPost("account-exists")]
    public async Task<ActionResult<AccountExistResponseDto>> AccountExists([FromBody] AccountExistRequestDto dto) =>
        Ok(await identityService.AccountExistsAsync(dto));

    [HttpPost("request-otp-for-register")]
    public async Task RequestOtpForRegister([FromBody] RequestOtpForRegisterRequestDto dto) =>
        await identityService.RequestOtpForRegisterAsync(dto);

    [HttpPost("register")]
    public async Task Register([FromBody] RegisterRequestDto dto) =>
        await identityService.RegisterAsync(dto);

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto dto) =>
        Ok(await identityService.LoginAsync(dto));

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto) =>
        Ok(await identityService.RefreshTokenAsync(dto));

    [HttpPost("logout")]
    public async Task Logout([FromBody] LogoutRequestDto dto) => await identityService.LogoutAsync(dto);
}