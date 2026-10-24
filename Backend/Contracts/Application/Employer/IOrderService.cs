using System.Security.Claims;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface IOrderService
{
    IQueryable<OrderSummaryResponseDto> GetMyOrdersQueryable(ClaimsPrincipal user);
    Task<OrderDetailResponseDto> GetOrderDetailAsync(ClaimsPrincipal user, Guid orderId);
    Task<OrderDetailResponseDto> CancelOrderAsync(ClaimsPrincipal user, Guid orderId);
}

public class OrderSummaryResponseDto
{
    public required Guid Id { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string Description { get; set; }
    public required OrderStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class OrderDetailResponseDto
{
    public required Guid Id { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string Description { get; set; }
    public required OrderStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiredAt { get; set; }
    public required DateTime? CompletedAt { get; set; }
    public required List<PaymentTransactionResponseDto> PaymentTransactions { get; set; }

    public class PaymentTransactionResponseDto
    {
        public required Guid Id { get; set; }
        public required PaymentProvider Provider { get; set; }
        public required decimal Amount { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime ExpiredAt { get; set; }
        public required DateTime? PaidAt { get; set; }
        public required PaymentTransactionStatus Status { get; set; }
        public string? PaymentUrl { get; set; }
    }
}