using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.ApplicationServices.Candidate;
using Service.ApplicationServices.Employer;
using Service.ApplicationServices.Public;
using Service.ExternalServices;

namespace Service;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Public Services
        services.AddScoped<JobService>();

        // Candidate Services
        services.AddScoped<CandidateProfileService>();
        services.AddScoped<JobReportService>();
        services.AddScoped<EmployerRegistrationService>();

        // Employer Services
        services.AddScoped<EmployerProfileService>();

        // Censor Services
        services.AddScoped<ApplicationServices.Censor.EmployerRegistrationService>();
        services.AddScoped<ApplicationServices.Censor.JobReportService>();

        // Cloudflare R2 Storage Service
        services.AddSingleton<CloudflareR2StorageService>();
    }
}