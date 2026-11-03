using System.ComponentModel.DataAnnotations;
using Domain.Shared.Constants;

namespace Contracts.Application.Manager;

public interface IViewJobStatsPackageManagementService
{
    Task<List<ManagedViewJobStatsPackageResponseDto>> GetPackagesAsync(CancellationToken cancellationToken = default);
    Task<ManagedViewJobStatsPackageResponseDto> GetPackageAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ManagedViewJobStatsPackageResponseDto> CreatePackageAsync(
        CreateUpdateViewJobStatsPackageRequestDto request, CancellationToken cancellationToken = default);
    Task<ManagedViewJobStatsPackageResponseDto> UpdatePackageAsync(Guid id,
        CreateUpdateViewJobStatsPackageRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivatePackageAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CreateUpdateViewJobStatsPackageRequestDto
{
    [Required(ErrorMessage = "Tên gói lượt xem là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.CandidateViewJobStatsPackage.NameMaxLength,
        ErrorMessage = "Tên gói lượt xem không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số lượt xem phải lớn hơn 0.")]
    public int ViewCredits { get; set; }

    [Range(typeof(decimal), "1", "9999999999999999", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Giá gói phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
    public decimal Price { get; set; }

    [MaxLength(BusinessRuleConstants.Models.CandidateViewJobStatsPackage.DescriptionMaxLength,
        ErrorMessage = "Mô tả gói không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ManagedViewJobStatsPackageResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int ViewCredits { get; set; }
    public required decimal Price { get; set; }
    public string? Description { get; set; }
    public required bool IsActive { get; set; }
    public required DateTime CreatedAt { get; set; }
}
