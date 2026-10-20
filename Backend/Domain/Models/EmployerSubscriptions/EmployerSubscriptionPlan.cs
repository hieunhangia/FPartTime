using Domain.Shared.Enums;

namespace Domain.Models.EmployerSubscriptions;

public class EmployerSubscriptionPlan
{
    public required EmployerSubscriptionTier Tier { get; set; }
    public required EmployerSubscriptionDuration Duration { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required decimal Price { get; set; }
    public required int DurationDays { get; set; }
    public required int ExtraActivePosts { get; set; }
    public required int FreeBumps { get; set; }
    public bool CanRecommendCandidates { get; set; }
    public bool CanQueryCandidates { get; set; }
    public bool IsActive { get; set; } = true;
}
