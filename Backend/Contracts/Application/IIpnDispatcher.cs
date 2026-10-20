namespace Contracts.Application;

public interface IIpnDispatcher
{
    Task DispatchAsync(string paymentReferenceId);
}