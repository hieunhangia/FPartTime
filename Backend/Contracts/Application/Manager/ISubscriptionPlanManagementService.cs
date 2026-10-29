using System.ComponentModel.DataAnnotations;
using Domain.Shared.Constants;
using Domain.Shared.Enums;

namespace Contracts.Application.Manager;

public interface ISubscriptionPlanManagementService
{
    Task<List<ManagedSubscriptionPlanResponseDto>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<ManagedSubscriptionPlanResponseDto> GetPlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, CancellationToken cancellationToken = default);
    Task<ManagedSubscriptionPlanResponseDto> CreatePlanAsync(CreateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ManagedSubscriptionPlanResponseDto> UpdatePlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, UpdateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken = default);
    Task DeactivatePlanAsync(EmployerSubscriptionTier tier, EmployerSubscriptionDuration duration,
        CancellationToken cancellationToken = default);
}

public class CreateSubscriptionPlanRequestDto : UpdateSubscriptionPlanRequestDto
{
    [Required(ErrorMessage = "Hạng gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Hạng gói không hợp lệ.")]
    public EmployerSubscriptionTier? Tier { get; set; }

    [Required(ErrorMessage = "Kỳ hạn gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn gói không hợp lệ.")]
    public EmployerSubscriptionDuration? Duration { get; set; }
}

public class UpdateSubscriptionPlanRequestDto
{
    [Required(ErrorMessage = "Tên gói là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.NameMaxLength,
        ErrorMessage = "Tên gói không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.DescriptionMaxLength,
        ErrorMessage = "Mô tả gói không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Giá gói phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
    public decimal Price { get; set; }

    [Range(1, BusinessRuleConstants.Models.EmployerSubscriptionPlan.MaxDurationDays,
        ErrorMessage = "Thời hạn gói phải từ 1 đến {2} ngày.")]
    public int DurationDays { get; set; }

    [Range(0, int.MaxValue - BusinessRuleConstants.Employers.DefaultMaxActivePosts,
        ErrorMessage = "Số tin đăng thêm không hợp lệ.")]
    public int ExtraActivePosts { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượt đẩy tin miễn phí không được âm.")]
    public int FreeBumps { get; set; }

    public bool CanRecommendCandidates { get; set; }
    public bool CanQueryCandidates { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ManagedSubscriptionPlanResponseDto
{
    public required EmployerSubscriptionTier Tier { get; set; }
    public required EmployerSubscriptionDuration Duration { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required decimal Price { get; set; }
    public required int DurationDays { get; set; }
    public required int ExtraActivePosts { get; set; }
    public required int FreeBumps { get; set; }
    public required bool CanRecommendCandidates { get; set; }
    public required bool CanQueryCandidates { get; set; }
    public required bool IsActive { get; set; }
}
