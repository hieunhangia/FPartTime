using ApiSdk;
using ApiSdk.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace BackendApiClient;

public class RefreshTokenApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public const string HttpClientName = "RefreshTokenApiClient";

    public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default)
    {
        using var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(),
            httpClient: httpClientFactory.CreateClient(HttpClientName));
        adapter.BaseUrl = configuration["BackendApi:BaseUrl"]!.TrimEnd('/');
        return await new ApiClient(adapter).Api.Identity.RefreshToken.PostAsync(
            new RefreshTokenRequestDto { RefreshToken = refreshToken }, cancellationToken: cancellationToken);
    }
}