using Contracts.External.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredPaymentTransactionCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ExpiredPaymentTransactionCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    $"{nameof(ExpiredPaymentTransactionCleanupService)} đang kiểm tra các giao dịch thanh toán hết hạn...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var cancelledCount = await scope.ServiceProvider.GetRequiredService<IPaymentService>()
                    .CancelExpiredTransactionsAsync(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (cancelledCount > 0)
                    {
                        logger.LogInformation("Đã tự động hủy {CancelledCount} giao dịch hết hạn thanh toán.",
                            cancelledCount);
                    }
                    else
                    {
                        logger.LogInformation("Không có giao dịch nào hết hạn.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi kiểm tra các giao dịch hết hạn.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}