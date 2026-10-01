using ApiSdk;
using BackendApiClient;
using Frontend.Filters;
using Microsoft.AspNetCore.Mvc;
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

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();