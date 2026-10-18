using Contracts.Application.Employer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredSubscriptionCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ExpiredSubscriptionCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    $"{nameof(ExpiredSubscriptionCleanupService)} đang kiểm tra các gói đăng ký hết hạn của các nhà tuyển dụng...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var expiredCount = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>()
                    .ProcessExpiredSubscriptionsAsync(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (expiredCount > 0)
                    {
                        logger.LogInformation("Đã xử lý {ExpiredCount} gói đăng ký hết hạn của các nhà tuyển dụng....",
                            expiredCount);
                    }
                    else
                    {
                        logger.LogInformation("Không có gói đăng ký nào hết hạn.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi kiểm tra các gói đăng ký hết hạn.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}