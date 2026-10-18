using System.Security.Claims;
using Contracts.External;
using Domain;
using Domain.Models.Users;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Service.Candidate;

public class CandidateProfileService(ApplicationDbContext dbContext, IObjectStorageService storage)
{
    private static readonly HashSet<string> AllowedImageExtensions = [".png", ".jpg", ".jpeg"];

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

        var dto = profile.MapToDto();
        if (!string.IsNullOrEmpty(profile.AvatarFilePath))
        {
            dto.AvatarFilePath = await storage.GetPublicUrlAsync(profile.AvatarFilePath);
        }

        return dto;
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

        if (request.AvatarFilePath != null)
        {
            var extension = Path.GetExtension(request.AvatarFilePath.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new BadRequestException("Chỉ chấp nhận file ảnh định dạng .png, .jpg hoặc .jpeg.");
            }

            if (request.AvatarFilePath.Length > 5 * 1024 * 1024)
            {
                throw new BadRequestException("Dung lượng ảnh tối đa là 5MB.");
            }
        }

        var profile = await dbContext.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (profile == null)
        {
            profile = new CandidateProfile { UserId = userId };
            dbContext.Candidates.Add(profile);
        }

        request.UpdateEntity(profile);

        if (request.AvatarFilePath is { Length: > 0 })
        {
            if (!string.IsNullOrEmpty(profile.AvatarFilePath))
            {
                await storage.DeleteFileAsync(profile.AvatarFilePath);
            }

            var extension = Path.GetExtension(request.AvatarFilePath.FileName).ToLowerInvariant();
            var newAvatarKey = $"candidate-avatars/{userId:N}_{Guid.NewGuid():N}{extension}";

            await using var stream = request.AvatarFilePath.OpenReadStream();
            await storage.UploadFileAsync(
                newAvatarKey,
                stream,
                request.AvatarFilePath.ContentType
            );

            profile.AvatarFilePath = newAvatarKey;
        }

        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(profile.PreferredWorkCommuneCode))
        {
            await dbContext.Entry(profile)
                .Reference(p => p.PreferredWorkCommune)
                .LoadAsync();
        }

        var dto = profile.MapToDto();
        if (!string.IsNullOrEmpty(profile.AvatarFilePath))
        {
            dto.AvatarFilePath = await storage.GetPublicUrlAsync(profile.AvatarFilePath);
        }

        return dto;
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
    public IFormFile? AvatarFilePath { get; set; }
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
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Name)}",
        nameof(CandidateProfileResponseDto.PreferredWorkCommuneName))]
    public static partial CandidateProfileResponseDto MapToDto(this CandidateProfile profile);

    [MapperIgnoreTarget(nameof(CandidateProfile.AvatarFilePath))]
    public static partial void UpdateEntity(this UpdateCandidateProfileDto request, CandidateProfile profile);
}
