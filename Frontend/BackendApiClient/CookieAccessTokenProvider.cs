using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;

namespace BackendApiClient;

public class CookieAccessTokenProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } =
        new(configuration["BackendApi:AccessTokenAllowedHosts"]!.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public Task<string> GetAuthorizationTokenAsync(Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedHostsValidator.IsUrlHostValid(uri))
        {
            return Task.FromResult(string.Empty);
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.FromResult(string.Empty);
        var accessToken = AuthCookieHelper.GetAccessToken(httpContext.Request);
        return Task.FromResult(string.IsNullOrEmpty(accessToken) ? string.Empty : accessToken);
    }
}