using Contracts.Application.Manager;
using Contracts.Common;
using Contracts.External;
using Domain;
using Domain.Models.Users;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Manager;

public class EmployerManagementService(ApplicationDbContext context, IObjectStorageService storage)
    : IEmployerManagementService
{
    public async Task<PagedResponseDto<ManagedEmployerSummaryResponseDto>> GetEmployersAsync(
        EmployerListRequestDto request, CancellationToken cancellationToken = default)
    {
        var query = context.Employers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            var matchesId = Guid.TryParse(searchTerm, out var employerId);
            query = query.Where(employer =>
                (matchesId && employer.UserId == employerId) ||
                employer.CompanyName.Contains(searchTerm) ||
                employer.TaxCode.Contains(searchTerm) ||
                (employer.ContactPhone != null && employer.ContactPhone.Contains(searchTerm)) ||
                (employer.User != null && employer.User.PhoneNumber != null &&
                 employer.User.PhoneNumber.Contains(searchTerm)));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var skip = (long)(request.PageNumber - 1) * request.PageSize;
        if (skip > int.MaxValue)
            return new PagedResponseDto<ManagedEmployerSummaryResponseDto>
            {
                TotalCount = totalCount,
                Items = []
            };

        var items = await query.OrderBy(employer => employer.CompanyName)
            .ThenBy(employer => employer.UserId)
            .Skip((int)skip).Take(request.PageSize)
            .ProjectToSummaryDto()
            .ToListAsync(cancellationToken);

        return new PagedResponseDto<ManagedEmployerSummaryResponseDto>
        {
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<ManagedEmployerDetailResponseDto> GetEmployerAsync(Guid employerId,
        CancellationToken cancellationToken = default)
    {
        var employer = await context.Employers.AsNoTracking()
            .Include(profile => profile.User)
            .FirstOrDefaultAsync(profile => profile.UserId == employerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nhà tuyển dụng.");

        var result = employer.MapToDetailDto();
        if (!string.IsNullOrEmpty(employer.LogoFilePath))
            result.LogoUrl = await storage.GetPublicUrlAsync(employer.LogoFilePath);

        return result;
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ManagedEmployerMapper
{
    [MapProperty(nameof(EmployerProfile.UserId), nameof(ManagedEmployerSummaryResponseDto.EmployerId))]
    [MapProperty($"{nameof(EmployerProfile.User)}.{nameof(User.PhoneNumber)}",
        nameof(ManagedEmployerSummaryResponseDto.PhoneNumber))]
    public static partial ManagedEmployerSummaryResponseDto MapToSummaryDto(this EmployerProfile employer);

    public static partial IQueryable<ManagedEmployerSummaryResponseDto> ProjectToSummaryDto(
        this IQueryable<EmployerProfile> employers);

    [MapProperty(nameof(EmployerProfile.UserId), nameof(ManagedEmployerDetailResponseDto.EmployerId))]
    [MapProperty($"{nameof(EmployerProfile.User)}.{nameof(User.PhoneNumber)}",
        nameof(ManagedEmployerDetailResponseDto.PhoneNumber))]
    [MapperIgnoreTarget(nameof(ManagedEmployerDetailResponseDto.LogoUrl))]
    public static partial ManagedEmployerDetailResponseDto MapToDetailDto(this EmployerProfile employer);
}
