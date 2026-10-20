using Contracts.Application;
using Domain;
using Domain.Models.Orders;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class PaymentTransactionService(ApplicationDbContext context) : IPaymentTransactionService
{
    public async Task<Guid> CreatePaymentTransactionAsync(Guid userId, string paymentReferenceId, decimal amount,
        DateTime expiredAt)
    {
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            PaymentReferenceId = paymentReferenceId,
            Amount = amount,
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

    public async Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default) =>
        await context.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Pending && t.ExpiredAt < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired), cancellationToken);
}