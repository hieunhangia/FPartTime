using Domain.Models.Users;
using Domain.Shared.Enums;

namespace Domain.Models.EmployerSubscriptions;

public class EmployerSubscription
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public required EmployerSubscriptionTier Tier { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public required DateTime EndDate { get; set; }
    public EmployerSubscriptionStatus Status { get; set; } = EmployerSubscriptionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required int ExtraActivePosts { get; set; }
    public required bool CanRecommendCandidates { get; set; }
    public required bool CanQueryCandidates { get; set; }
    public EmployerProfile? Employer { get; set; }
}
