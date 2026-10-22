namespace Contracts.Application;

public interface IPaymentTransactionService
{
    Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default);
}