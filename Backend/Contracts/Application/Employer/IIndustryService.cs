using System.ComponentModel.DataAnnotations;
using Domain.Shared.Constants;

namespace Contracts.Application.Employer;

public interface IIndustryService
{
    IQueryable<IndustryDetailResponseDto> GetIndustriesQueryable();
    Task<IndustryDetailResponseDto> GetIndustryDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IndustryDetailResponseDto> CreateIndustryAsync(CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken = default);
    Task<IndustryDetailResponseDto> UpdateIndustryAsync(Guid id, CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken = default);
}

public class CreateUpdateIndustryRequestDto
{
    [Required(ErrorMessage = "Tên ngành nghề là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.Industry.NameMaxLength,
        ErrorMessage = "Tên ngành nghề không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class IndustryDetailResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required bool IsActive { get; set; }
}
