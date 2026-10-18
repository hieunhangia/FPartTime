using Contracts.Application.Employer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredSubscriptionOrderCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ExpiredSubscriptionOrderCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    $"{nameof(ExpiredSubscriptionOrderCleanupService)} đang kiểm tra các đơn hàng thanh toán gói đăng ký hết hạn...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var cancelledCount = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>()
                    .CancelExpiredOrdersAsync(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (cancelledCount > 0)
                    {
                        logger.LogInformation("Đã tự động hủy {CancelledCount} đơn hàng hết hạn thanh toán.",
                            cancelledCount);
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
