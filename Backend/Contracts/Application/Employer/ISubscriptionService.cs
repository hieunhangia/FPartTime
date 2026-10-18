using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface ISubscriptionService
{
    Task<List<EmployerSubscriptionPlanResponseDto>> GetPlansAsync();
    Task<CurrentEmployerSubscriptionResponseDto> GetCurrentSubscriptionAsync(ClaimsPrincipal user);
    Task CancelCurrentSubscriptionAsync(ClaimsPrincipal user);

    Task<CreateSubscriptionCheckoutResponseDto> CreateCheckoutAsync(ClaimsPrincipal user,
        CreateSubscriptionCheckoutRequestDto request);

    Task ProcessIpnAsync(string paymentReferenceId);
    Task<int> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<int> CancelExpiredOrdersAsync(CancellationToken cancellationToken = default);
}

public class EmployerSubscriptionPlanResponseDto
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
}

public class CreateSubscriptionCheckoutRequestDto
{
    [Required(ErrorMessage = "Gói dịch vụ là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Gói dịch vụ không hợp lệ.")]
    public EmployerSubscriptionTier? Tier { get; set; }

    [Required(ErrorMessage = "Kỳ hạn gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn gói không hợp lệ.")]
    public EmployerSubscriptionDuration? Duration { get; set; }

    [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc.")]
    [EnumDataType(typeof(PaymentProvider), ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public PaymentProvider? PaymentProvider { get; set; }

    [Required(ErrorMessage = "Đường dẫn quay về (ReturnUrl) là bắt buộc.")]
    [Url(ErrorMessage = "Đường dẫn quay về phải là URL hợp lệ.")]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn khi hủy (CancelUrl) là bắt buộc.")]
    [Url(ErrorMessage = "Đường dẫn khi hủy phải là URL hợp lệ.")]
    public string CancelUrl { get; set; } = string.Empty;
}

public class CreateSubscriptionCheckoutResponseDto
{
    public required string PaymentReferenceId { get; set; }
    public required string PaymentUrl { get; set; }
}

public class CurrentEmployerSubscriptionResponseDto
{
    public EmployerSubscriptionTier? Tier { get; set; }
    public EmployerSubscriptionDuration? Duration { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required bool HasActiveSubscription { get; set; }
    public int ExtraActivePosts { get; set; }
    public bool CanRecommendCandidates { get; set; }
    public bool CanQueryCandidates { get; set; }
}