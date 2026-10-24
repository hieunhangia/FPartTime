using System.Net.Http.Headers;
using ApiSdk;
using BackendApiClient;
using BackendApiClient.Extensions;
using Frontend.Filters;
using Frontend.Services.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secret.json", optional: true, reloadOnChange: true);

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
                    await context.HttpContext.SignOutApiTokenAsync();
                }

                if (new JsonWebTokenHandler().ReadJsonWebToken(accessToken).ValidTo < DateTime.UtcNow)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutApiTokenAsync();
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAccessTokenProvider, CookieAccessTokenProvider>();
builder.Services.AddScoped<IAuthenticationProvider>(sp =>
{
    var tokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
    return new BaseBearerTokenAuthenticationProvider(tokenProvider);
});
builder.Services.AddHttpClient<ApiClient>()
    .AddTypedClient<ApiClient>((httpClient, sp) =>
    {
        var authProvider = sp.GetRequiredService<IAuthenticationProvider>();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = builder.Configuration["BackendApi:BaseUrl"]!.TrimEnd('/')
        };

        return new ApiClient(adapter);
    });

builder.Services.AddHttpClient(SystemHealthService.BetterStackClientName, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BetterStack:ApiBaseUrl"]!.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        builder.Configuration["BetterStack:ApiToken"] ??
        throw new InvalidOperationException("BetterStack:ApiToken is not configured."));
});

builder.Services.AddScoped<SystemHealthService>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Public/Error");
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