using Domain.Shared.Enums;

namespace Contracts.Application;

public interface IPaymentTransactionService
{
    Task<Guid> CreatePaymentTransactionAsync(Guid userId, PaymentPurpose purpose, PaymentProvider provider,
        string paymentReferenceId, decimal amount, string description, DateTime? expiredAt = null);

    Task CancelPaymentTransactionAsync(Guid id);
    Task<PaymentPurpose?> GetPaymentPurposeAsync(string paymentReferenceId, PaymentProvider provider);
    Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default);
}