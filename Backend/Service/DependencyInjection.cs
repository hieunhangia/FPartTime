using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Service.ApplicationServices;
using Service.BackgroundServices;
using Service.ExternalServices;

namespace Service;

public static class DependencyInjection
{
    public static void AddServiceLevelServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<JsonWebTokenHandler>();
        services.AddScoped<IdentityService>();

        services.AddScoped<SmsSenderService>();

        var r2Settings = configuration.GetSection("R2Settings");
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(r2Settings["AccessKey"], r2Settings["SecretKey"],
            new AmazonS3Config { ServiceURL = r2Settings["ServiceURL"] }
        ));
        services.AddSingleton<CloudflareR2StorageService>();

        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<ExpiredJobCleanupService>();
    }
}