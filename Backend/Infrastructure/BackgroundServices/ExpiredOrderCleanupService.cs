using Contracts.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredOrderCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ExpiredOrderCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    $"{nameof(ExpiredOrderCleanupService)} đang kiểm tra các đơn hàng hết hạn...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var expiredCount = await scope.ServiceProvider.GetRequiredService<IOrderService>()
                    .ExpireOrdersAsync(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (expiredCount > 0)
                    {
                        logger.LogInformation("Đã tự động chuyển trạng thái {ExpiredCount} đơn hàng hết hạn.",
                            expiredCount);
                    }
                    else
                    {
                        logger.LogInformation("Không có đơn hàng nào hết hạn.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi kiểm tra các đơn hàng hết hạn.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
