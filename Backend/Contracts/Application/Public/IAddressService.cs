namespace Contracts.Application.Public;

public interface IAddressService
{
    Task<List<ProvinceDto>> GetProvincesAsync();
    Task<List<CommuneDto>> GetCommunesByProvinceCodeAsync(string provinceCode);
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