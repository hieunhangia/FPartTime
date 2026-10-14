using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Service.ApplicationServices;
using Service.ApplicationServices.Candidate;
using Service.ApplicationServices.Employer;
using Service.ApplicationServices.Public;
using Service.BackgroundServices;
using Service.ExternalServices;
using EmployerRegistrationService = Service.ApplicationServices.Censor.EmployerRegistrationService;

namespace Service;

public static class DependencyInjection
{
    public static void AddServiceLevelServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<JsonWebTokenHandler>();
        services.AddScoped<IdentityService>();
        services.AddScoped<NotificationsService>();

        // Public Services
        services.AddScoped<AddressService>();
        services.AddScoped<IndustryService>();

        // Candidate Services
        services.AddScoped<ApplicationServices.Candidate.EmployerRegistrationService>();

        // Employer Services
        services.AddScoped<JobService>();

        // Censor Services
        services.AddScoped<EmployerRegistrationService>();

        services.AddScoped<SmsSenderService>();
        services.AddScoped<CandidateProfileService>();
        var r2Settings = configuration.GetSection("R2Settings");
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(r2Settings["AccessKey"], r2Settings["SecretKey"],
            new AmazonS3Config { ServiceURL = r2Settings["ServiceURL"] }
        ));
        services.AddSingleton<CloudflareR2StorageService>();
        services.AddHealthChecks().AddCheck<CloudflareR2HealthCheck>(name: "Cloudflare R2 Storage", tags: ["storage"]);

        services.AddHostedService<ExpiredJobCleanupService>();
    }
}