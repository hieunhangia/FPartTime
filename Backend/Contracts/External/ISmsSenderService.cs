namespace Contracts.External;

public interface ISmsSenderService
{
    Task SendAsync(SendSmsRequest request);
}

public class SendSmsRequest
{
    public required string PhoneNumber { get; set; }
    public required string Message { get; set; }
}