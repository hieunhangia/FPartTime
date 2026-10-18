using Domain.Models.Users;
using Domain.Shared.Constants;
using Domain.Shared.Enums;

namespace Domain.Models.EmployerSubscriptions;

public class EmployerSubscriptionOrder
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public required PaymentProvider PaymentProvider { get; set; }
    public required string PaymentReferenceId { get; set; }
    public required EmployerSubscriptionTier Tier { get; set; }
    public required EmployerSubscriptionDuration Duration { get; set; }
    public required decimal Amount { get; set; }
    public EmployerSubscriptionOrderStatus Status { get; set; } = EmployerSubscriptionOrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiredAt { get; set; } =
        DateTime.UtcNow.AddMinutes(BusinessRuleConstants.Employers.Subscriptions.OrderExpiresInMinutes);

    public DateTime? PaidAt { get; set; }

    public EmployerProfile? Employer { get; set; }
}