using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using ApiSdk;
using ApiSdk.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace BackendApiClient;

public class RefreshTokenDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    RefreshTokenApiClient refreshTokenApiClient,
    ILogger<RefreshTokenDelegatingHandler> logger)
    : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> IsRefreshRequestKey = new("IsRefreshRequest");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return response;
        }

        var refreshToken = AuthCookieHelper.GetRefreshToken(httpContext.Request);
        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            request.Options.TryGetValue(IsRefreshRequestKey, out var isRefresh) && isRefresh ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        TokenResponseDto? tokenResponse = null;
        try
        {
            tokenResponse = await refreshTokenApiClient.RefreshTokenAsync(refreshToken, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Đã xảy ra lỗi khi gọi API làm mới token: {Message}", ex.Message);
        }

        if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null)
        {
            AuthCookieHelper.ClearAuthCookies(httpContext.Response);
            return response;
        }

        AuthCookieHelper.SetAuthCookies(httpContext.Response, tokenResponse);
        response.Dispose();
        var retryRequest = await CloneHttpRequestMessageAsync(request);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }


    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in original.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        clone.Options.Set(IsRefreshRequestKey, true);
        clone.Version = original.Version;
        return clone;
    }
}

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