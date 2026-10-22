using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface ISubscriptionService
{
    Task<List<EmployerSubscriptionPlanResponseDto>> GetPlansAsync();
    Task<CurrentEmployerSubscriptionResponseDto> GetCurrentSubscriptionAsync(ClaimsPrincipal user);
    Task CancelCurrentSubscriptionAsync(ClaimsPrincipal user);

    Task<CreateCheckoutResponseDto> CreatePayOSCheckoutAsync(ClaimsPrincipal user,
        CreatePayOSCheckoutRequestDto request);

    Task<CreateCheckoutResponseDto> CreateVNPAYCheckoutAsync(ClaimsPrincipal user,
        CreateVNPAYCheckoutRequestDto request);

    Task ProcessSubscriptionOrderAsync(Guid transactionId);
    Task<int> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
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

public abstract class SubscriptionCheckoutRequestBase
{
    [Required(ErrorMessage = "Gói dịch vụ là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Gói dịch vụ không hợp lệ.")]
    public EmployerSubscriptionTier? Tier { get; set; }

    [Required(ErrorMessage = "Kỳ hạn gói là bắt buộc.")]
    [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn gói không hợp lệ.")]
    public EmployerSubscriptionDuration? Duration { get; set; }

    [Required(ErrorMessage = "Đường dẫn quay về (ReturnPath) là bắt buộc.")]
    [RegularExpression(@"^/(?![/\\]).*$",
        ErrorMessage = "Đường dẫn quay về (ReturnPath) phải là đường dẫn tương đối hợp lệ bắt đầu bằng '/'.")]
    public string ReturnPath { get; set; } = string.Empty;
}

public class CreatePayOSCheckoutRequestDto : SubscriptionCheckoutRequestBase
{
    [Required(ErrorMessage = "Đường dẫn khi hủy (CancelPath) là bắt buộc.")]
    [RegularExpression(@"^/(?![/\\]).*$",
        ErrorMessage = "Đường dẫn khi hủy (CancelPath) phải là đường dẫn tương đối hợp lệ bắt đầu bằng '/'.")]
    public string CancelPath { get; set; } = string.Empty;
}

public class CreateVNPAYCheckoutRequestDto : SubscriptionCheckoutRequestBase
{
    [Required(ErrorMessage = "Địa chỉ IP là bắt buộc.")]
    [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Địa chỉ IP không hợp lệ.")]
    public string IpAddress { get; set; } = string.Empty;
}

public class CreateCheckoutResponseDto
{
    public required string PaymentUrl { get; set; }
}

public class CurrentEmployerSubscriptionResponseDto
{
    public EmployerSubscriptionTier? Tier { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required bool HasActiveSubscription { get; set; }
    public int ExtraActivePosts { get; set; }
    public bool CanRecommendCandidates { get; set; }
    public bool CanQueryCandidates { get; set; }
}