using Contracts.Application;
using Domain;
using Domain.Models.Payments;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class PaymentTransactionService(ApplicationDbContext context) : IPaymentTransactionService
{
    public async Task<Guid> CreatePaymentTransactionAsync(Guid userId, PaymentPurpose purpose, PaymentProvider provider,
        string paymentReferenceId, decimal amount, string description, DateTime? expiredAt = null)
    {
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            Purpose = purpose,
            Provider = provider,
            PaymentReferenceId = paymentReferenceId,
            Amount = amount,
            Description = description,
            ExpiredAt = expiredAt
        };
        context.PaymentTransactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction.Id;
    }

    public async Task CancelPaymentTransactionAsync(Guid id) =>
        await context.PaymentTransactions
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Cancelled));

    public async Task<PaymentPurpose?> GetPaymentPurposeAsync(string paymentReferenceId, PaymentProvider provider) =>
        await context.PaymentTransactions
            .Where(t => t.PaymentReferenceId == paymentReferenceId && t.Provider == provider)
            .Select(t => t.Purpose)
            .FirstOrDefaultAsync();

    public async Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default) =>
        await context.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Pending && t.ExpiredAt < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired), cancellationToken);
}