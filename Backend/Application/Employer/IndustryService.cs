using Contracts.Application.Employer;
using Domain;
using Domain.Models.Jobs;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class IndustryService(ApplicationDbContext context) : IIndustryService
{
    public IQueryable<IndustryDetailResponseDto> GetIndustriesQueryable() =>
        context.Industries.AsNoTracking().ProjectToDto();

    public async Task<IndustryDetailResponseDto> GetIndustryDetailAsync(Guid id,
        CancellationToken cancellationToken = default) =>
        await context.Industries.AsNoTracking()
            .Where(i => i.Id == id)
            .ProjectToDto()
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Không tìm thấy ngành nghề.");

    public async Task<IndustryDetailResponseDto> CreateIndustryAsync(CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = await ValidateNameAsync(request.Name, null, cancellationToken);
        var industry = new Industry { Name = name, IsActive = request.IsActive };
        context.Industries.Add(industry);
        await context.SaveChangesAsync(cancellationToken);
        return industry.MapToDto();
    }

    public async Task<IndustryDetailResponseDto> UpdateIndustryAsync(Guid id, CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var industry = await context.Industries.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Không tìm thấy ngành nghề cần cập nhật.");

        industry.Name = await ValidateNameAsync(request.Name, id, cancellationToken);
        industry.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return industry.MapToDto();
    }

    private async Task<string> ValidateNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Tên ngành nghề là bắt buộc.");
        }

        if (name.Length > BusinessRuleConstants.Models.Industry.NameMaxLength)
        {
            throw new BadRequestException($"Tên ngành nghề không được vượt quá {BusinessRuleConstants.Models.Industry.NameMaxLength} ký tự.");
        }

        var trimmedName = name.Trim();
        var normalizedName = trimmedName.ToUpper();
        if (await context.Industries.AnyAsync(
                i => (!excludedId.HasValue || i.Id != excludedId.Value) && i.Name.ToUpper() == normalizedName,
                cancellationToken))
        {
            throw new ConflictException("Tên ngành nghề đã tồn tại.");
        }

        return trimmedName;
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IndustryMapper
{
    public static partial IQueryable<IndustryDetailResponseDto> ProjectToDto(this IQueryable<Industry> industry);
    public static partial IndustryDetailResponseDto MapToDto(this Industry industry);
}
