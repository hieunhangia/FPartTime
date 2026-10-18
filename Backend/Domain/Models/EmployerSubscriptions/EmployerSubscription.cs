using Domain.Models.Users;
using Domain.Shared.Enums;

namespace Domain.Models.EmployerSubscriptions;

public class EmployerSubscription
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public required EmployerSubscriptionTier Tier { get; set; }
    public required EmployerSubscriptionDuration Duration { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public EmployerSubscriptionStatus Status { get; set; } = EmployerSubscriptionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EmployerProfile? Employer { get; set; }
}
