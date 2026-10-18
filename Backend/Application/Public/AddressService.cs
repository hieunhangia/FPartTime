using Contracts.Application.Public;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Models.Address;
using Riok.Mapperly.Abstractions;

namespace Application.Public;

public class AddressService(ApplicationDbContext dbContext) : IAddressService
{
    public async Task<List<ProvinceDto>> GetProvincesAsync() =>
        await dbContext.Provinces.ProjectToDto().ToListAsync();

    public async Task<List<CommuneDto>> GetCommunesByProvinceCodeAsync(string provinceCode) =>
        await dbContext.Communes.Where(d => d.ProvinceCode == provinceCode).ProjectToDto().ToListAsync();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class AddressMapper
{
    public static partial IQueryable<ProvinceDto> ProjectToDto(this IQueryable<Province> provinces);
    public static partial IQueryable<CommuneDto> ProjectToDto(this IQueryable<Commune> communes);
}
