using Contracts.Application.Manager;
using Domain;
using Domain.Models.CandidateViewJobStats;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Manager;

public class ViewJobStatsPackageManagementService(ApplicationDbContext context)
    : IViewJobStatsPackageManagementService
{
    public async Task<List<ManagedViewJobStatsPackageResponseDto>> GetPackagesAsync(
        CancellationToken cancellationToken = default) =>
        await context.CandidateViewJobStatsPackages.AsNoTracking()
            .OrderBy(package => package.Price)
            .ThenBy(package => package.Id)
            .ProjectToManagedPackageDto()
            .ToListAsync(cancellationToken);

    public async Task<ManagedViewJobStatsPackageResponseDto> GetPackageAsync(Guid id,
        CancellationToken cancellationToken = default) =>
        await context.CandidateViewJobStatsPackages.AsNoTracking()
            .Where(package => package.Id == id)
            .ProjectToManagedPackageDto()
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Không tìm thấy gói lượt xem.");

    public async Task<ManagedViewJobStatsPackageResponseDto> CreatePackageAsync(
        CreateUpdateViewJobStatsPackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        ValidateValues(request);

        var package = new CandidateViewJobStatsPackage
        {
            Name = name,
            ViewCredits = request.ViewCredits,
            Price = request.Price,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive
        };

        context.CandidateViewJobStatsPackages.Add(package);
        await context.SaveChangesAsync(cancellationToken);
        return package.MapToManagedPackageDto();
    }

    public async Task<ManagedViewJobStatsPackageResponseDto> UpdatePackageAsync(Guid id,
        CreateUpdateViewJobStatsPackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        ValidateValues(request);

        var package = await context.CandidateViewJobStatsPackages.FindAsync([id], cancellationToken)
                      ?? throw new NotFoundException("Không tìm thấy gói lượt xem cần cập nhật.");

        package.Name = name;
        package.ViewCredits = request.ViewCredits;
        package.Price = request.Price;
        package.Description = NormalizeDescription(request.Description);
        package.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return package.MapToManagedPackageDto();
    }

    public async Task DeactivatePackageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await context.CandidateViewJobStatsPackages.FindAsync([id], cancellationToken)
                      ?? throw new NotFoundException("Không tìm thấy gói lượt xem cần ngừng cung cấp.");

        if (!package.IsActive) return;
        package.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string ValidateName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BadRequestException("Tên gói lượt xem là bắt buộc.");
        }

        if (trimmed.Length > BusinessRuleConstants.Models.CandidateViewJobStatsPackage.NameMaxLength)
        {
            throw new BadRequestException("Tên gói lượt xem quá dài.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidateValues(CreateUpdateViewJobStatsPackageRequestDto request)
    {
        if (request.ViewCredits <= 0 ||
            request.Price is < 1m or > 9999999999999999m ||
            decimal.Truncate(request.Price) != request.Price ||
            request.Description?.Length > BusinessRuleConstants.Models.CandidateViewJobStatsPackage.DescriptionMaxLength)
        {
            throw new BadRequestException("Số lượt xem, giá hoặc mô tả gói không hợp lệ.");
        }
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ManagedViewJobStatsPackageMapper
{
    public static partial IQueryable<ManagedViewJobStatsPackageResponseDto> ProjectToManagedPackageDto(
        this IQueryable<CandidateViewJobStatsPackage> packages);

    public static partial ManagedViewJobStatsPackageResponseDto MapToManagedPackageDto(
        this CandidateViewJobStatsPackage package);
}
