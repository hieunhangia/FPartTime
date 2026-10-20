using Domain.Shared.Enums;

namespace Contracts.Application;

public interface IPaymentTransactionService
{
    Task<Guid> CreatePaymentTransactionAsync(Guid userId, string paymentReferenceId, decimal amount,
        DateTime expiredAt);

    Task CancelPaymentTransactionAsync(Guid id);
    Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default);
}