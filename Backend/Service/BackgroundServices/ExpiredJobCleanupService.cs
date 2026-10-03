using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository;
using Repository.Models.Jobs;

namespace Service.BackgroundServices;

public class ExpiredJobCleanupService(IServiceProvider serviceProvider, ILogger<ExpiredJobCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation($"{nameof(ExpiredJobCleanupService)} đang kiểm tra và đóng các job hết hạn...");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
                var updatedCount = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Jobs
                    .Where(x => x.Status == JobStatus.Active && x.ExpiryAt < currentDate)
                    .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, JobStatus.Closed), stoppingToken);
                
                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (updatedCount > 0)
                    {
                        logger.LogInformation("Đã đóng thành công {UpdatedCount} job hết hạn.", updatedCount);
                    }
                    else
                    {
                        logger.LogInformation("Không có job nào hết hạn cần đóng.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi đóng các job hết hạn.");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
