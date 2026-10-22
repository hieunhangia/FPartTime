using Amazon.S3;
using Contracts.External;
using Contracts.External.Payments;
using Infrastructure.BackgroundServices;
using Infrastructure.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        // Background Services
        builder.Services.AddHostedService<ExpiredJobCleanupService>();
        builder.Services.AddHostedService<ExpiredSubscriptionCleanupService>();
        builder.Services.AddHostedService<ExpiredPaymentTransactionCleanupService>();

        // Payment Services
        builder.Services.AddScoped<IPaymentServiceFactory, PaymentServiceFactory>();
        // PayOS
        builder.Services.AddScoped<IPaymentService, PayOSPaymentService>();
        builder.Services.AddHttpClient(PayOSHealthCheck.HealthCheckHttpClientName, client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["PayOS:HealthCheckUrl"] ??
                                         throw new InvalidOperationException("PayOS:HealthCheckUrl is not configured"));
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddHealthChecks().AddCheck<PayOSHealthCheck>(name: "PayOS Service", tags: ["payment"]);

        // VNPAY
        builder.Services.AddScoped<IPaymentService, VNPAYPaymentService>();
        builder.Services.AddHealthChecks().AddCheck<VNPAYHealthCheck>(name: "VNPAY Service", tags: ["payment"]);

        // Sms Sender Service
        builder.Services.AddSingleton<ISmsSenderService, LoggerSmsSenderService>();

        // Cloudflare R2 Storage Service
        var r2Settings = builder.Configuration.GetSection("R2Settings");
        builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(r2Settings["AccessKey"], r2Settings["SecretKey"],
            new AmazonS3Config { ServiceURL = r2Settings["ServiceURL"] }
        ));
        builder.Services.AddSingleton<IObjectStorageService, CloudflareR2StorageService>();
        builder.Services.AddHealthChecks()
            .AddCheck<CloudflareR2HealthCheck>(name: "Cloudflare R2 Storage", tags: ["storage"]);
    }
}