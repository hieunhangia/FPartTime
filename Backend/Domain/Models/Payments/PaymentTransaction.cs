using Domain.Models.EmployerSubscriptions;
using Domain.Models.Users;
using Domain.Shared.Enums;

namespace Domain.Models.Payments;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required PaymentPurpose Purpose { get; set; }
    public required PaymentProvider Provider { get; set; }
    public required string PaymentReferenceId { get; set; }
    public required decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiredAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public User? User { get; set; }
    public EmployerSubscriptionOrder? EmployerSubscriptionOrder { get; set; }
}