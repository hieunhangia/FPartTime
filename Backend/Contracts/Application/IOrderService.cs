namespace Contracts.Application;

public interface IOrderService
{
    Task<int> ExpireOrdersAsync(CancellationToken cancellationToken = default);
}
