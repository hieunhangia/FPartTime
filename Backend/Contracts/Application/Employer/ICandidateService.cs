using System.Security.Claims;
using Contracts.Common;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface ICandidateService
{
    Task<PagedResponseDto<RecommendedCandidateResponseDto>> GetRecommendedCandidatesAsync(
        ClaimsPrincipal user,
        Guid jobId,
        PaginationRequestDto? pagination = null);
    Task<IQueryable<CandidateSummaryResponseDto>> SearchCandidatesQueryableAsync(ClaimsPrincipal user);
    Task<CandidateDetailResponseDto> GetCandidateDetailAsync(ClaimsPrincipal user, Guid candidateId);
}

public class RecommendedCandidateResponseDto
{
    public required Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
    public string? PreferredWorkCommuneName { get; set; }
    public string? PreferredWorkProvinceName { get; set; }
    public List<CandidateScheduleResponseDto> PreferredWorkSchedules { get; set; } = [];
    public int MatchScore { get; set; }
    public List<string> MatchReasons { get; set; } = [];
}

public class CandidateSummaryResponseDto
{
    public required Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
    public string? PreferredWorkCommuneName { get; set; }
    public string? PreferredWorkProvinceName { get; set; }
}

public class CandidateDetailResponseDto
{
    public required Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
    public string? PreferredWorkCommuneName { get; set; }
    public string? PreferredWorkProvinceName { get; set; }
    public List<CandidateScheduleResponseDto> PreferredWorkSchedules { get; set; } = [];
}
