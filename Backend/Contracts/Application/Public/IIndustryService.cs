using System.ComponentModel.DataAnnotations;
using Domain.Shared.Constants;

namespace Contracts.Application.Public;

public interface IIndustryService
{
    Task<List<IndustryResponseDto>> GetAllIndustriesAsync();
    Task<List<PopularIndustryResponseDto>> GetPopularIndustriesAsync(int limit,
        CancellationToken cancellationToken = default);
}

public class IndustryResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class GetPopularIndustriesRequestDto
{
    [Range(1, BusinessRuleConstants.Industries.MaxPopularLimit,
        ErrorMessage = "Số lượng ngành nghề phải từ {1} đến {2}.")]
    public int Limit { get; set; } = BusinessRuleConstants.Industries.DefaultPopularLimit;
}

public class PopularIndustryResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int JobCount { get; set; }
}
