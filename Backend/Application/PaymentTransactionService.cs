using Contracts.Application;
using Domain;
using Domain.Models.Orders;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class PaymentTransactionService(ApplicationDbContext context) : IPaymentTransactionService
{
    public async Task<int> CancelExpiredTransactionsAsync(CancellationToken cancellationToken = default) =>
        await context.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Pending && t.ExpiredAt < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired), cancellationToken);
}