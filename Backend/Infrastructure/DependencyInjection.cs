using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayOS;
using PayOS.Models.Webhooks;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        // PayOS Service
        builder.Services.AddSingleton(new PayOSClient(new PayOSOptions
        {
            ClientId = builder.Configuration["PayOS:ClientId"],
            ApiKey = builder.Configuration["PayOS:ApiKey"],
            ChecksumKey = builder.Configuration["PayOS:ChecksumKey"]
        }));
        builder.Services.AddSingleton<IPaymentService, PayOSPaymentService>();
        builder.Services.AddSingleton<IPaymentIpnService<Webhook, WebhookData>, PayOSWebhookService>();
        builder.Services.AddHttpClient(PayOSHealthCheck.HealthCheckHttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api-merchant.payos.vn/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddHealthChecks().AddCheck<PayOSHealthCheck>(name: "PayOS Service", tags: ["payment"]);
    }
}