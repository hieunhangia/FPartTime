using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Jobs;
using Riok.Mapperly.Abstractions;

namespace Service.ApplicationServices.Public;

public class IndustryService(ApplicationDbContext dbContext)
{
    public async Task<List<IndustryResponseDto>> GetAllIndustriesAsync() =>
        await dbContext.Industries.ProjectToDto().ToListAsync();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IndustryMapper
{
    public static partial IQueryable<IndustryResponseDto> ProjectToDto(this IQueryable<Industry> industry);
}

public class IndustryResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}