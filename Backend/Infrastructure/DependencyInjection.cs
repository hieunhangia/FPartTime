using Amazon.S3;
using Contracts.External;
using Contracts.External.Payment;
using Infrastructure.BackgroundServices;
using Infrastructure.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayOS;
using PayOS.Models.Webhooks;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        // Background Services
        builder.Services.AddHostedService<ExpiredJobCleanupService>();
        builder.Services.AddHostedService<ExpiredSubscriptionCleanupService>();
        builder.Services.AddHostedService<ExpiredPaymentTransactionCleanupService>();

        // PayOS Service
        builder.Services.AddSingleton(new PayOSClient(new PayOSOptions
        {
            ClientId = builder.Configuration["PayOS:ClientId"],
            ApiKey = builder.Configuration["PayOS:ApiKey"],
            ChecksumKey = builder.Configuration["PayOS:ChecksumKey"]
        }));
        builder.Services.AddScoped<IPaymentService, PayOSPaymentService>();
        builder.Services.AddScoped<IPaymentServiceFactory, PaymentServiceFactory>();
        builder.Services.AddHttpClient(PayOSHealthCheck.HealthCheckHttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api-merchant.payos.vn/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddHealthChecks().AddCheck<PayOSHealthCheck>(name: "PayOS Service", tags: ["payment"]);

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