using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BackendApiClient.Extensions;

public static class HttpContextApiAuthExtensions
{
    public const string AccessTokenName = "access_token";
    public const string RefreshTokenName = "refresh_token";

    extension(HttpContext httpContext)
    {
        public async Task SignInWithApiTokenAsync(string accessToken, string refreshToken)
        {
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken)) return;
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(400)
            };
            authProperties.StoreTokens(
            [
                new AuthenticationToken { Name = AccessTokenName, Value = accessToken },
                new AuthenticationToken { Name = RefreshTokenName, Value = refreshToken }
            ]);
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(
                new ClaimsIdentity(new JsonWebTokenHandler().ReadJsonWebToken(accessToken).Claims,
                    CookieAuthenticationDefaults.AuthenticationScheme)), authProperties);
        }

        public async Task SignOutApiTokenAsync() =>
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        public async Task<string?> GetAccessTokenAsync() => await httpContext.GetTokenAsync(AccessTokenName);

        public async Task<string?> GetRefreshTokenAsync() => await httpContext.GetTokenAsync(RefreshTokenName);
    }
}