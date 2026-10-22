using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Contracts.Application.Candidate;

public interface ICandidateProfileService
{
    Task<OpenForJobStatusResponseDto> ToggleOpenForJobStatusAsync(ClaimsPrincipal user,
        ToggleOpenForJobStatusRequestDto request);

    Task<OpenForJobStatusResponseDto> GetOpenForJobStatusAsync(ClaimsPrincipal user);
}

public class ToggleOpenForJobStatusRequestDto
{
    [Required] public bool Enable { get; set; }
}

public class OpenForJobStatusResponseDto
{
    public required DateTime? OpenForJobUntil { get; set; }
    public required bool IsOpenForJob { get; set; }
    public required bool CanRefresh { get; set; }
}