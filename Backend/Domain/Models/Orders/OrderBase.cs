using Domain.Models.Users;
using Domain.Shared.Enums;

namespace Domain.Models.Orders;

public abstract class OrderBase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string Description { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}