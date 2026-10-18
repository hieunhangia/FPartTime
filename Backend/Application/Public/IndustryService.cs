using Contracts.Application.Public;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Jobs;
using Riok.Mapperly.Abstractions;

namespace Application.Public;

public class IndustryService(ApplicationDbContext dbContext) : IIndustryService
{
    public async Task<List<IndustryResponseDto>> GetAllIndustriesAsync() =>
        await dbContext.Industries.ProjectToDto().ToListAsync();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IndustryMapper
{
    public static partial IQueryable<IndustryResponseDto> ProjectToDto(this IQueryable<Industry> industry);
}
