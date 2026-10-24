using Contracts.Application;
using Domain;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class OrderService(ApplicationDbContext context) : IOrderService
{
    public async Task<int> ExpireOrdersAsync(CancellationToken cancellationToken = default) =>
        await context.Orders
            .Where(o => o.Status == OrderStatus.PendingPayment && o.ExpiredAt < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Expired), cancellationToken);
}
