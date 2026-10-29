using System.ComponentModel.DataAnnotations;
using ApiSdk.Models;
using Frontend.Constants;

namespace Frontend.ViewModels.Manager;

public class SubscriptionPlanFormInput
{
    [Required(ErrorMessage = "Tên gói là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.NameMaxLength,
        ErrorMessage = "Tên gói không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.DescriptionMaxLength,
        ErrorMessage = "Mô tả không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Giá gói là bắt buộc.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Giá gói phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Thời hạn là bắt buộc.")]
    [Range(1, BusinessRuleConstants.Models.EmployerSubscriptionPlan.MaxDurationDays,
        ErrorMessage = "Thời hạn phải từ 1 đến {2} ngày.")]
    public int? DurationDays { get; set; }

    [Required(ErrorMessage = "Số tin đăng thêm là bắt buộc.")]
    [Range(0, BusinessRuleConstants.Models.EmployerSubscriptionPlan.MaxExtraActivePosts,
        ErrorMessage = "Số tin đăng thêm không hợp lệ.")]
    public int? ExtraActivePosts { get; set; } = 0;

    [Required(ErrorMessage = "Số lượt đẩy tin là bắt buộc.")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượt đẩy tin không được âm.")]
    public int? FreeBumps { get; set; } = 0;

    public bool CanRecommendCandidates { get; set; }
    public bool CanQueryCandidates { get; set; }
    public bool IsActive { get; set; } = true;

    public CreateSubscriptionPlanRequestDto ToCreateRequest(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration) => new()
    {
        Tier = tier,
        Duration = duration,
        Name = Name.Trim(),
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        Price = (double)Price!.Value,
        DurationDays = DurationDays,
        ExtraActivePosts = ExtraActivePosts,
        FreeBumps = FreeBumps,
        CanRecommendCandidates = CanRecommendCandidates,
        CanQueryCandidates = CanQueryCandidates,
        IsActive = IsActive
    };

    public UpdateSubscriptionPlanRequestDto ToUpdateRequest() => new()
    {
        Name = Name.Trim(),
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        Price = (double)Price!.Value,
        DurationDays = DurationDays,
        ExtraActivePosts = ExtraActivePosts,
        FreeBumps = FreeBumps,
        CanRecommendCandidates = CanRecommendCandidates,
        CanQueryCandidates = CanQueryCandidates,
        IsActive = IsActive
    };

    public static SubscriptionPlanFormInput FromResponse(ManagedSubscriptionPlanResponseDto plan) => new()
    {
        Name = plan.Name ?? string.Empty,
        Description = plan.Description,
        Price = plan.Price is { } price ? (decimal)price : null,
        DurationDays = plan.DurationDays,
        ExtraActivePosts = plan.ExtraActivePosts,
        FreeBumps = plan.FreeBumps,
        CanRecommendCandidates = plan.CanRecommendCandidates ?? false,
        CanQueryCandidates = plan.CanQueryCandidates ?? false,
        IsActive = plan.IsActive ?? false
    };
}

public static class SubscriptionPlanLabels
{
    public static string Tier(EmployerSubscriptionTier? tier) => tier switch
    {
        EmployerSubscriptionTier.Plus => "Plus",
        EmployerSubscriptionTier.Pro => "Pro",
        EmployerSubscriptionTier.Ultra => "Ultra",
        _ => "Không xác định"
    };

    public static string Duration(EmployerSubscriptionDuration? duration) => duration switch
    {
        EmployerSubscriptionDuration.Monthly => "Hàng tháng",
        EmployerSubscriptionDuration.Yearly => "Hàng năm",
        _ => "Không xác định"
    };
}
