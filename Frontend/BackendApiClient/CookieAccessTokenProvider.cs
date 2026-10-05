using BackendApiClient.Extensions;
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

    public async Task<string> GetAuthorizationTokenAsync(Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedHostsValidator.IsUrlHostValid(uri))
        {
            return string.Empty;
        }

        var accessToken = await httpContextAccessor.HttpContext.GetAccessTokenAsync();
        return string.IsNullOrEmpty(accessToken) ? string.Empty : accessToken;
    }
}