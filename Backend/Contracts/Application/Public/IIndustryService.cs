namespace Contracts.Application.Public;

public interface IIndustryService
{
    Task<List<IndustryResponseDto>> GetAllIndustriesAsync();
}

public class IndustryResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
