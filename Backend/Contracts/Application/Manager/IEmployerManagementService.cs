using System.ComponentModel.DataAnnotations;
using Contracts.Common;
using Domain.Shared.Constants;

namespace Contracts.Application.Manager;

public interface IEmployerManagementService
{
    Task<PagedResponseDto<ManagedEmployerSummaryResponseDto>> GetEmployersAsync(
        EmployerListRequestDto request, CancellationToken cancellationToken = default);
    Task<ManagedEmployerDetailResponseDto> GetEmployerAsync(Guid employerId,
        CancellationToken cancellationToken = default);
}

public class EmployerListRequestDto : PaginationRequestDto
{
    [MaxLength(BusinessRuleConstants.Models.Employer.CompanyNameMaxLength,
        ErrorMessage = "Từ khóa tìm kiếm không được vượt quá {1} ký tự.")]
    public string? SearchTerm { get; set; }
}

public class ManagedEmployerSummaryResponseDto
{
    public required Guid EmployerId { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPhone { get; set; }
    public required int MaxActivePosts { get; set; }
    public required int BumpBalance { get; set; }
}

public class ManagedEmployerDetailResponseDto
{
    public required Guid EmployerId { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPhone { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public required int MaxActivePosts { get; set; }
    public required int BumpBalance { get; set; }
}
