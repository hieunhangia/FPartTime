using Contracts.Application;
using Contracts.Application.Employer;
using Domain;
using Domain.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class IpnDispatcher(
    ApplicationDbContext context,
    ISubscriptionService subscriptionService,
    INotificationsService notificationsService,
    ILogger<IpnDispatcher> logger) : IIpnDispatcher
{
    public async Task DispatchAsync(string paymentReferenceId)
    {
        var transaction = await context.PaymentTransactions.AsNoTracking()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.PaymentReferenceId == paymentReferenceId);
        if (transaction == null)
        {
            logger.LogError("Không tìm thấy giao dịch với mã tham chiếu: {PaymentReferenceId}", paymentReferenceId);
            return;
        }

        switch (transaction.Order)
        {
            case EmployerSubscriptionOrder:
                await subscriptionService.ProcessSubscriptionOrderAsync(transaction.Id);
                break;
            case null:
                logger.LogError("Giao dịch {TransactionId} không có đơn hàng liên kết.", transaction.Id);
                await notificationsService.CreateNotificationAsync(transaction.UserId,
                    "Giao dịch không có đơn hàng liên kết",
                    $"Giao dịch {transaction.Id} không có đơn hàng liên kết. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
                break;
            default:
                logger.LogWarning("Chưa hỗ trợ xử lý loại đơn: {OrderType}", transaction.Order.GetType().Name);
                await notificationsService.CreateNotificationAsync(transaction.UserId,
                    "Chưa hỗ trợ xử lý loại đơn",
                    $"Giao dịch {transaction.Id} có loại đơn {transaction.Order.GetType().Name} chưa được hỗ trợ. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
                break;
        }
    }
}