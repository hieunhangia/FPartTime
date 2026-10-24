using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<JsonWebTokenHandler>();
        builder.Services.AddSingleton<Contracts.Application.IFrontendUrlBuilder, FrontendUrlBuilder>();
        builder.Services.AddScoped<Contracts.Application.IIdentityService, IdentityService>();
        builder.Services.AddScoped<Contracts.Application.INotificationsService, NotificationsService>();
        builder.Services.AddScoped<Contracts.Application.IPaymentTransactionService, PaymentTransactionService>();
        builder.Services.AddScoped<Contracts.Application.IOrderService, OrderService>();
        builder.Services.AddScoped<Contracts.Application.IIpnDispatcher, IpnDispatcher>();

        // Public
        builder.Services.AddScoped<Contracts.Application.Public.IAddressService, Public.AddressService>();
        builder.Services.AddScoped<Contracts.Application.Public.IIndustryService, Public.IndustryService>();

        // Candidate
        builder.Services.AddScoped<Contracts.Application.Candidate.ICandidateProfileService,
            Candidate.CandidateProfileService>();

        // Employer
        builder.Services.AddScoped<Contracts.Application.Employer.IJobService, Employer.JobService>();
        builder.Services.AddScoped<Contracts.Application.Employer.ISubscriptionService, Employer.SubscriptionService>();
        builder.Services.AddScoped<Contracts.Application.Employer.ICandidateService, Employer.CandidateService>();
        builder.Services.AddScoped<Contracts.Application.Employer.IOrderService, Employer.OrderService>();
        builder.Services.AddScoped<Contracts.Application.Employer.IPaymentTransactionService,
            Employer.PaymentTransactionService>();

        // Censor

        // Manager
        builder.Services.AddScoped<Contracts.Application.Manager.IIndustryService, Manager.IndustryService>();

        // Admin
    }
}