using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace Contracts.Application.Manager;

public interface IEmployerSubscriptionManagementService
{
    Task<List<ManagedEmployerSubscriptionResponseDto>> GetSubscriptionsAsync(Guid employerId,
        CancellationToken cancellationToken = default);
    Task<ManagedEmployerSubscriptionResponseDto> GetSubscriptionAsync(Guid employerId, Guid subscriptionId,
        CancellationToken cancellationToken = default);
    Task<ManagedEmployerSubscriptionResponseDto> GrantAsync(Guid employerId,
        GrantEmployerSubscriptionRequestDto request, CancellationToken cancellationToken = default);
    Task<ManagedEmployerSubscriptionResponseDto> UpdatePeriodAsync(Guid employerId, Guid subscriptionId,
        UpdateEmployerSubscriptionPeriodRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ManagedEmployerSubscriptionResponseDto> UpdateTierAsync(Guid employerId, Guid subscriptionId,
        UpdateEmployerSubscriptionTierRequestDto request,
        CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid employerId, Guid subscriptionId, CancellationToken cancellationToken = default);
}

public class GrantEmployerSubscriptionRequestDto
{
    [Required(ErrorMessage = "Hạng gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Hạng gói không hợp lệ.")]
    public EmployerSubscriptionTier? Tier { get; set; }

    [Required(ErrorMessage = "Kỳ hạn gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn gói không hợp lệ.")]
    public EmployerSubscriptionDuration? Duration { get; set; }
}

public class UpdateEmployerSubscriptionPeriodRequestDto
{
    [Required(ErrorMessage = "Ngày hết hạn là bắt buộc.")]
    public DateTimeOffset? EndDate { get; set; }
}

public class UpdateEmployerSubscriptionTierRequestDto : GrantEmployerSubscriptionRequestDto;

public class ManagedEmployerSubscriptionResponseDto
{
    public required Guid Id { get; set; }
    public required Guid EmployerId { get; set; }
    public required EmployerSubscriptionTier Tier { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required EmployerSubscriptionStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int ExtraActivePosts { get; set; }
    public required bool CanRecommendCandidates { get; set; }
    public required bool CanQueryCandidates { get; set; }
}
