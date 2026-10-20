using Application.Employer;
using Application.Public;
using Contracts.Application;
using Contracts.Application.Employer;
using Contracts.Application.Public;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IFrontendUrlBuilder, FrontendUrlBuilder>();
        builder.Services.AddSingleton<JsonWebTokenHandler>();
        builder.Services.AddScoped<IIdentityService, IdentityService>();
        builder.Services.AddScoped<INotificationsService, NotificationsService>();
        builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();

        // Public
        builder.Services.AddScoped<IAddressService, AddressService>();
        builder.Services.AddScoped<Contracts.Application.Public.IIndustryService, Public.IndustryService>();

        // Candidate

        // Employer
        builder.Services.AddScoped<Contracts.Application.Employer.IIndustryService, Employer.IndustryService>();
        builder.Services.AddScoped<IJobService, JobService>();
        builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
        builder.Services.AddScoped<ICandidateService, CandidateService>();

        // Censor

        // Manager

        // Admin
    }
}
