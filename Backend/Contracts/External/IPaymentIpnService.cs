namespace Contracts.External;

public interface IPaymentIpnService<in TRequest, TResponse>
{
    Task<TResponse> ProcessIpnAsync(TRequest request);
}