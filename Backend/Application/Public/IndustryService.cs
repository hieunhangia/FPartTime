using Contracts.Application.Public;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Models.Jobs;
using Riok.Mapperly.Abstractions;

namespace Application.Public;

public class IndustryService(ApplicationDbContext dbContext) : IIndustryService
{
    public async Task<List<IndustryResponseDto>> GetAllIndustriesAsync() =>
        await dbContext.Industries.AsNoTracking().Where(i => i.IsActive).ProjectToDto().ToListAsync();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IndustryMapper
{
    public static partial IQueryable<IndustryResponseDto> ProjectToDto(this IQueryable<Industry> industry);
}
