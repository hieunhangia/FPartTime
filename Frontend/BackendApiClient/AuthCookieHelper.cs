using ApiSdk.Models;
using Microsoft.AspNetCore.Http;

namespace BackendApiClient;

public static class AuthCookieHelper
{
    private const string AccessTokenCookieName = "AccessToken";
    private const string RefreshTokenCookieName = "RefreshToken";

    public static void SetAuthCookies(HttpResponse response, TokenResponseDto tokenResponse)
    {
        if (tokenResponse.AccessToken is null || tokenResponse.RefreshToken is null) return;
        response.Cookies.Append(AccessTokenCookieName, tokenResponse.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(400)
        });
        response.Cookies.Append(RefreshTokenCookieName, tokenResponse.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(400)
        });
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete(AccessTokenCookieName);
        response.Cookies.Delete(RefreshTokenCookieName);
    }

    public static string? GetAccessToken(HttpRequest request) =>
        request.Cookies.TryGetValue(AccessTokenCookieName, out var token) ? token : null;

    public static string? GetRefreshToken(HttpRequest request) =>
        request.Cookies.TryGetValue(RefreshTokenCookieName, out var token) ? token : null;
}