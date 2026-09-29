using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository;

namespace Service.BackgroundServices;

public class RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("RefreshTokenCleanupService đang dọn dẹp các token hết hạn...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var deletedCount = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokens
                    .Where(x => x.ExpiryDate < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (deletedCount > 0)
                    {
                        logger.LogInformation("Đã xóa thành công {DeletedCount} Refresh Token hết hạn.", deletedCount);
                    }
                    else
                    {
                        logger.LogInformation("Không có Refresh Token nào hết hạn để xóa.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi dọn dẹp Refresh Token.");
            }

            await Task.Delay(TimeSpan.FromHours(BusinessRuleConstants.Identity.RefreshTokenCleanupIntervalInHours),
                stoppingToken);
        }
    }
}