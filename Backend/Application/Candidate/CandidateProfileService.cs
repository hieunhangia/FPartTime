using System.Security.Claims;
using Contracts.Application.Candidate;
using Domain;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Application.Candidate;

public class CandidateProfileService(ApplicationDbContext dbContext) : ICandidateProfileService
{
    public async Task<OpenForJobStatusResponseDto> GetOpenForJobStatusAsync(ClaimsPrincipal user)
    {
        var profile = await dbContext.Candidates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == user.GetUserId());

        return profile == null
            ? throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.")
            : BuildStatusDto(profile.OpenForJobUntil);
    }

    public async Task<OpenForJobStatusResponseDto> ToggleOpenForJobStatusAsync(ClaimsPrincipal user,
        ToggleOpenForJobStatusRequestDto request)
    {
        var profile = await dbContext.Candidates
            .FirstOrDefaultAsync(c => c.UserId == user.GetUserId());
        if (profile == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        var now = DateTime.UtcNow;
        if (request.Enable)
        {
            if (profile.OpenForJobUntil.HasValue && profile.OpenForJobUntil.Value > now)
            {
                var activatedDate = profile.OpenForJobUntil.Value
                    .AddDays(-BusinessRuleConstants.Candidates.OpenForJobAvailabilityDays)
                    .Date;

                if (now.Date == activatedDate)
                {
                    throw new BadRequestException(
                        "Bạn không thể làm mới trong cùng ngày vừa kích hoạt hoặc làm mới. Vui lòng quay lại vào ngày mai.");
                }
            }

            profile.OpenForJobUntil = now.AddDays(BusinessRuleConstants.Candidates.OpenForJobAvailabilityDays);
        }
        else
        {
            profile.OpenForJobUntil = null;
        }

        await dbContext.SaveChangesAsync();
        return BuildStatusDto(profile.OpenForJobUntil);
    }

    private static OpenForJobStatusResponseDto BuildStatusDto(DateTime? openForJobUntil)
    {
        var now = DateTime.UtcNow;
        var isOpen = openForJobUntil.HasValue && openForJobUntil.Value > now;

        var canRefresh = false;
        if (isOpen)
        {
            var activatedDate = openForJobUntil!.Value
                .AddDays(-BusinessRuleConstants.Candidates.OpenForJobAvailabilityDays)
                .Date;
            canRefresh = now.Date > activatedDate;
        }

        return new OpenForJobStatusResponseDto
        {
            OpenForJobUntil = openForJobUntil,
            IsOpenForJob = isOpen,
            CanRefresh = canRefresh
        };
    }
}