using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Users;
using Riok.Mapperly.Abstractions;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices.Candidate;

public class CandidateProfileService(ApplicationDbContext dbContext)
{
    public async Task<CandidateProfileResponseDto> GetProfileAsync(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();

        var profile = await dbContext.Candidates
            .Include(c => c.PreferredWorkCommune)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (profile == null)
        {
            throw new NotFoundException("Hồ sơ chưa được tạo.");
        }

        return profile.MapToDto();
    }

    public async Task<CandidateProfileResponseDto> EditCandidateProfile(
        ClaimsPrincipal user,
        UpdateCandidateProfileDto request)
    {
        var userId = user.GetUserId();

        if (!string.IsNullOrWhiteSpace(request.PreferredWorkCommuneCode))
        {
            var communeExist = await dbContext.Communes
                .AnyAsync(c => c.Code == request.PreferredWorkCommuneCode);

            if (!communeExist)
            {
                throw new NotFoundException("Xã/Phường không tồn tại.");
            }
        }

        var profile = await dbContext.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (profile == null)
        {
            profile = new CandidateProfile { UserId = userId };
            request.UpdateEntity(profile);
            dbContext.Candidates.Add(profile);
        }
        else
        {
            request.UpdateEntity(profile);
        }

        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(profile.PreferredWorkCommuneCode))
        {
            await dbContext.Entry(profile)
                .Reference(p => p.PreferredWorkCommune)
                .LoadAsync();
        }

        return profile.MapToDto();
    }
}

public class CandidateProfileResponseDto
{
    public string? AvatarFilePath { get; set; }
    public string? Bio { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
    public string? PreferredWorkCommuneName { get; set; }
}

public class UpdateCandidateProfileDto
{
    public string? AvatarFilePath { get; set; }
    public string? Bio { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class CandidateProfileMapper
{
    [MapProperty(
        nameof(CandidateProfile.PreferredWorkCommune.Name),
        nameof(CandidateProfileResponseDto.PreferredWorkCommuneName))]
    public static partial CandidateProfileResponseDto MapToDto(this CandidateProfile profile);

    public static partial void UpdateEntity(this UpdateCandidateProfileDto request, CandidateProfile profile);
}
