using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using PayOS;
using Service.ApplicationServices;
using Service.ApplicationServices.Candidate;
using Service.ApplicationServices.Employer;
using Service.ApplicationServices.Public;
using Service.BackgroundServices;
using Service.ExternalServices;

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
        //services.AddScoped<JobService>();

        // Candidate Services
        services.AddScoped<CandidateProfileService>();
        services.AddScoped<JobReportService>();
        services.AddScoped<EmployerRegistrationService>();

        // Employer Services
        services.AddScoped<ApplicationServices.Employer.JobService>();
        services.AddScoped<EmployerProfileService>();

        // Censor Services
        services.AddScoped<ApplicationServices.Censor.EmployerRegistrationService>();
        services.AddScoped<ApplicationServices.Censor.JobReportService>();

        // Sms Sender Service
        services.AddSingleton<SmsSenderService>();

        // Cloudflare R2 Storage Service
        var r2Settings = configuration.GetSection("R2Settings");
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(r2Settings["AccessKey"], r2Settings["SecretKey"],
            new AmazonS3Config { ServiceURL = r2Settings["ServiceURL"] }
        ));
        services.AddSingleton<CloudflareR2StorageService>();
        services.AddHealthChecks().AddCheck<CloudflareR2HealthCheck>(name: "Cloudflare R2 Storage", tags: ["storage"]);

        // PayOS Service
        services.AddHttpClient(PayOSHealthCheck.HealthCheckHttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api-merchant.payos.vn/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton(new PayOSClient(new PayOSOptions
        {
            ClientId = configuration["PayOS:ClientId"],
            ApiKey = configuration["PayOS:ApiKey"],
            ChecksumKey = configuration["PayOS:ChecksumKey"]
        }));
        services.AddSingleton<PayOSService>();
        services.AddHealthChecks().AddCheck<PayOSHealthCheck>(name: "PayOS Service", tags: ["payment"]);

        // Background Services
        services.AddHostedService<ExpiredJobCleanupService>();
    }
}