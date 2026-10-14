using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Address;
using Riok.Mapperly.Abstractions;

namespace Service.ApplicationServices.Public;

public class AddressService(ApplicationDbContext dbContext)
{
    public async Task<List<ProvinceDto>> GetProvincesAsync() => await dbContext.Provinces.ProjectToDto().ToListAsync();

    public async Task<List<CommuneDto>> GetCommunesByProvinceCodeAsync(string provinceCode) =>
        await dbContext.Communes.Where(d => d.ProvinceCode == provinceCode).ProjectToDto().ToListAsync();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class AddressMapper
{
    public static partial IQueryable<ProvinceDto> ProjectToDto(this IQueryable<Province> provinces);
    public static partial IQueryable<CommuneDto> ProjectToDto(this IQueryable<Commune> communes);
}

public class ProvinceDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public class CommuneDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string ProvinceCode { get; set; }
}