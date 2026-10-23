using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.Candidate;
using Service.Employer;
using Service.Public;

namespace Service;

public static class DependencyInjection
{
    public static void AddApplicationServices_Old(this IHostApplicationBuilder builder)
    {
        // Public Services
        builder.Services.AddScoped<JobService>();

        // Candidate Services
        builder.Services.AddScoped<CandidateProfileService>();
        builder.Services.AddScoped<JobReportService>();
        builder.Services.AddScoped<EmployerRegistrationService>();
        builder.Services.AddScoped<CandidateScheduleService>();
        builder.Services.AddScoped<JobApplicationService>();


        // Employer Services
        builder.Services.AddScoped<EmployerProfileService>();

        // Censor Services
        builder.Services.AddScoped<Censor.EmployerRegistrationService>();
        builder.Services.AddScoped<Censor.JobReportService>();
    }
}