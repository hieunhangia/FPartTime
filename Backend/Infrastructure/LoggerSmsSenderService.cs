using Contracts.External;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public class LoggerSmsSenderService(ILogger<LoggerSmsSenderService> logger) : ISmsSenderService
{
    public async Task SendAsync(SendSmsRequest request)
    {
        logger.LogWarning("[Giả lập] Gửi SMS đến {PhoneNumber}: {Message}", request.PhoneNumber, request.Message);
        await Task.CompletedTask;
    }
}