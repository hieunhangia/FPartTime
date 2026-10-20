using Domain.Models.Users;
using Domain.Shared.Enums;

namespace Domain.Models.Orders;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string PaymentReferenceId { get; set; }
    public required decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required DateTime ExpiredAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public User? User { get; set; }
    public OrderBase? Order { get; set; }
}