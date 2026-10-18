using System.Security.Claims;
using System.Text.RegularExpressions;
using Contracts.External;
using Domain;
using Domain.Models.Users;
using Domain.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Service.Employer
{
    public class EmployerProfileService(ApplicationDbContext dbContext, IObjectStorageService storage)
    {
        private static readonly HashSet<string> AllowedImageExtensions = [".png", ".jpg", ".jpeg"];
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;

        public async Task<EmployerProfileResponseDto> GetProfileAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();

            var profile = await dbContext.Employers
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
            {
                throw new Exception("Hồ sơ nhà tuyển dụng chưa được tạo.");
            }

            var dto = profile.MapToDto();
            if (!string.IsNullOrEmpty(profile.LogoFilePath))
            {
                dto.LogoFilePath = await storage.GetPublicUrlAsync(profile.LogoFilePath);
            }

            return dto;
        }

        public async Task<EmployerProfileResponseDto> EditEmployerProfile(
            ClaimsPrincipal user,
            UpdateEmployerProfileDto request)
        {
            var userId = user.GetUserId();

            var profile = await dbContext.Employers
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
            {
                throw new Exception("Hồ sơ nhà tuyển dụng không tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            {
                if (!Regex.IsMatch(request.ContactPhone.Trim(), @"^(0)(3|5|7|8|9)[0-9]{8}$"))
                {
                    throw new Exception("Số điện thoại liên hệ không hợp lệ.");
                }
            }

            if (request.LogoFile != null && request.LogoFile.Length > 0)
            {
                var extension = Path.GetExtension(request.LogoFile.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    throw new Exception("Chỉ chấp nhận file ảnh định dạng .png, .jpg hoặc .jpeg.");
                }

                if (request.LogoFile.Length > MaxFileSizeInBytes)
                {
                    throw new Exception("Dung lượng logo tối đa là 5MB.");
                }
            }

            request.UpdateEntity(profile);

            if (request.LogoFile is { Length: > 0 })
            {
                if (!string.IsNullOrEmpty(profile.LogoFilePath))
                {
                    await storage.DeleteFileAsync(profile.LogoFilePath);
                }

                var extension = Path.GetExtension(request.LogoFile.FileName).ToLowerInvariant();
                var newLogoKey = $"employer-logos/{userId:N}_{Guid.NewGuid():N}{extension}";

                await using var stream = request.LogoFile.OpenReadStream();
                await storage.UploadFileAsync(
                    newLogoKey,
                    stream,
                    request.LogoFile.ContentType
                );

                profile.LogoFilePath = newLogoKey;
            }

            await dbContext.SaveChangesAsync();

            var dto = profile.MapToDto();
            if (!string.IsNullOrEmpty(profile.LogoFilePath))
            {
                dto.LogoFilePath = await storage.GetPublicUrlAsync(profile.LogoFilePath);
            }

            return dto;
        }
    }


    public class EmployerProfileResponseDto
    {
        public string? LogoFilePath { get; set; }
        public string? Description { get; set; }
        public string? ContactPhone { get; set; }
    }

    public class UpdateEmployerProfileDto
    {
        public IFormFile? LogoFile { get; set; }
        public string? Description { get; set; }
        public string? ContactPhone { get; set; }
    }


    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class EmployerProfileMapper
    {
        public static partial EmployerProfileResponseDto MapToDto(this EmployerProfile profile);

        [MapperIgnoreTarget(nameof(EmployerProfile.LogoFilePath))]
        public static partial void UpdateEntity(this UpdateEmployerProfileDto request, EmployerProfile profile);
    }
}