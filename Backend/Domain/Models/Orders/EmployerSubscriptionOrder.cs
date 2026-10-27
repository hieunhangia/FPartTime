using Domain.Shared.Enums;

namespace Domain.Models.Orders;

public class EmployerSubscriptionOrder : OrderBase
{
    public required EmployerSubscriptionTier Tier { get; set; }
    public required EmployerSubscriptionDuration Duration { get; set; }
    public required string PlanName { get; set; }
    public required int DurationDays { get; set; }
    public required int ExtraActivePosts { get; set; }
    public required int FreeBumps { get; set; }
    public required bool CanRecommendCandidates { get; set; }
    public required bool CanQueryCandidates { get; set; }
}