using Microsoft.Extensions.Logging;

namespace Service.ExternalServices;

public class SmsSenderService(ILogger<SmsSenderService> logger)
{
    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        logger.LogWarning("[Giả lập] Gửi SMS đến {PhoneNumber}: {Message}", phoneNumber, message);
        await Task.CompletedTask;
    }
}