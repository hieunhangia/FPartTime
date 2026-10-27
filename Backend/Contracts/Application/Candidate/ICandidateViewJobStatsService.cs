using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Contracts.Application.Candidate;

public interface ICandidateViewJobStatsService
{
    Task<CandidateViewJobStatsBalanceResponseDto> GetBalanceAsync(ClaimsPrincipal user);
    Task<List<CandidateViewJobStatsPackageResponseDto>> GetPackagesAsync();
    Task<CandidateJobStatsResponseDto> GetJobStatsAsync(ClaimsPrincipal user, Guid jobId);
    Task<CandidateJobStatsResponseDto> UnlockOrRefreshJobStatsAsync(ClaimsPrincipal user, Guid jobId);

    Task<CandidateViewJobStatsCheckoutResponseDto> CreatePayOSCheckoutAsync(ClaimsPrincipal user,
        CreateCandidateViewPayOSCheckoutRequestDto request);

    Task<CandidateViewJobStatsCheckoutResponseDto> CreateVNPAYCheckoutAsync(ClaimsPrincipal user,
        string clientIpAddress, CreateCandidateViewVNPAYCheckoutRequestDto request);

    Task<CandidateViewJobStatsCheckoutResponseDto> CreateZaloPayCheckoutAsync(ClaimsPrincipal user,
        CreateCandidateViewZaloPayCheckoutRequestDto request);

    Task ProcessOrderPaymentAsync(Guid transactionId);
}

public class CandidateViewJobStatsBalanceResponseDto
{
    public required int Balance { get; set; }
}

public class CandidateViewJobStatsPackageResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int ViewCredits { get; set; }
    public required decimal Price { get; set; }
    public string? Description { get; set; }
}

public class CandidateJobStatsResponseDto
{
    public required Guid JobId { get; set; }
    public required bool HasUnlocked { get; set; }
    public required int CurrentBalance { get; set; }
    public required bool IsJobActiveAndOpen { get; set; }
    public CandidateJobStatsSnapshotDto? Snapshot { get; set; }
}

public class CandidateJobStatsSnapshotDto
{
    public required int TotalApplications { get; set; }
    public required int PendingApplications { get; set; }
    public required int AcceptedApplications { get; set; }
    public required int RejectedApplications { get; set; }
    public required DateTime SnapshotAt { get; set; }
}

public abstract class CandidateViewCheckoutRequestBase
{
    [Required(ErrorMessage = "Gói lượt xem là bắt buộc.")]
    public Guid PackageId { get; set; }

    [Required(ErrorMessage = "Đường dẫn quay về (ReturnPath) là bắt buộc.")]
    [RegularExpression(@"^/(?![/\\]).*$",
        ErrorMessage = "Đường dẫn quay về (ReturnPath) phải là đường dẫn tương đối hợp lệ bắt đầu bằng '/'.")]
    public string ReturnPath { get; set; } = string.Empty;
}

public class CreateCandidateViewPayOSCheckoutRequestDto : CandidateViewCheckoutRequestBase
{
    [Required(ErrorMessage = "Đường dẫn khi hủy (CancelPath) là bắt buộc.")]
    [RegularExpression(@"^/(?![/\\]).*$",
        ErrorMessage = "Đường dẫn khi hủy (CancelPath) phải là đường dẫn tương đối hợp lệ bắt đầu bằng '/'.")]
    public string CancelPath { get; set; } = string.Empty;
}

public class CreateCandidateViewVNPAYCheckoutRequestDto : CandidateViewCheckoutRequestBase;

public class CreateCandidateViewZaloPayCheckoutRequestDto : CandidateViewCheckoutRequestBase;

public class CandidateViewJobStatsCheckoutResponseDto
{
    public required string PaymentUrl { get; set; }
}
