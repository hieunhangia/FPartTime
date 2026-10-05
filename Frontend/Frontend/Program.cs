using ApiSdk;
using BackendApiClient;
using BackendApiClient.Extensions;
using Frontend.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention("/", model =>
        model.Filters.Add(new TypeFilterAttribute(typeof(ApiUnauthorizedRedirectFilter))));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Identity/LoginOrRegister";
        options.AccessDeniedPath = "/";

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var accessToken = context.Properties.GetTokenValue(HttpContextApiAuthExtensions.AccessTokenName);
                if (string.IsNullOrEmpty(accessToken))
                {
                    context.RejectPrincipal();
                    return;
                }

                if (new JsonWebTokenHandler().ReadJsonWebToken(accessToken).ValidTo < DateTime.UtcNow)
                {
                    var refreshToken = context.Properties.GetTokenValue(HttpContextApiAuthExtensions.RefreshTokenName);
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        context.RejectPrincipal();
                        return;
                    }

                    try
                    {
                        var tokenResponse = await context.HttpContext.RequestServices
                            .GetRequiredService<RefreshTokenApiClient>().RefreshTokenAsync(refreshToken);
                        if (tokenResponse is { AccessToken: not null, RefreshToken: not null })
                        {
                            context.Properties.UpdateTokenValue(HttpContextApiAuthExtensions.AccessTokenName,
                                tokenResponse.AccessToken);
                            context.Properties.UpdateTokenValue(HttpContextApiAuthExtensions.RefreshTokenName,
                                tokenResponse.RefreshToken);
                            context.ShouldRenew = true;
                        }
                        else
                        {
                            context.RejectPrincipal();
                        }
                    }
                    catch
                    {
                        context.RejectPrincipal();
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient(RefreshTokenApiClient.HttpClientName);
builder.Services.AddTransient<RefreshTokenApiClient>();
builder.Services.AddTransient<RefreshTokenDelegatingHandler>();
builder.Services.AddScoped<IAccessTokenProvider, CookieAccessTokenProvider>();
builder.Services.AddScoped<IAuthenticationProvider>(sp =>
{
    var tokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
    return new BaseBearerTokenAuthenticationProvider(tokenProvider);
});
builder.Services.AddHttpClient<ApiClient>()
    .AddHttpMessageHandler<RefreshTokenDelegatingHandler>()
    .AddTypedClient<ApiClient>((httpClient, sp) =>
    {
        var authProvider = sp.GetRequiredService<IAuthenticationProvider>();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = builder.Configuration["BackendApi:BaseUrl"]!.TrimEnd('/')
        };

        return new ApiClient(adapter);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.Run();