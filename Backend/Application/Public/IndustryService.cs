using Contracts.Application.Public;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Models.Jobs;
using Domain.Shared.Enums;
using Riok.Mapperly.Abstractions;

namespace Application.Public;

public class IndustryService(ApplicationDbContext dbContext) : IIndustryService
{
    public async Task<List<IndustryResponseDto>> GetAllIndustriesAsync() =>
        await dbContext.Industries.AsNoTracking().Where(i => i.IsActive).ProjectToDto().ToListAsync();

    public async Task<List<PopularIndustryResponseDto>> GetPopularIndustriesAsync(int limit,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await dbContext.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Active && j.ExpiryAt >= today && j.Industry!.IsActive)
            .GroupBy(j => new { j.IndustryId, j.Industry!.Name })
            .Select(group => new PopularIndustryResponseDto
            {
                Id = group.Key.IndustryId,
                Name = group.Key.Name,
                JobCount = group.Count()
            })
            .OrderByDescending(i => i.JobCount)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IndustryMapper
{
    public static partial IQueryable<IndustryResponseDto> ProjectToDto(this IQueryable<Industry> industry);
}
