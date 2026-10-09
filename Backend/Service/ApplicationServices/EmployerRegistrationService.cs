using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Users;
using Riok.Mapperly.Abstractions;
using Service.ExternalServices;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices;

public class EmployerRegistrationService(
    ApplicationDbContext dbContext,
    CloudflareR2StorageService storageService)
{
    public async Task<EmployerRegistrationResponseDto> RegisterAsync(ClaimsPrincipal user,
        EmployerRegistrationRequestDto dto)
    {
        var userId = user.GetUserId();

        if (await dbContext.Employers.AnyAsync(e => e.UserId == userId))
        {
            throw new ConflictException("Tài khoản của bạn đã là nhà tuyển dụng.");
        }

        if (await dbContext.EmployerRegistrationRequests.AnyAsync(r =>
                r.RequesterId == userId && r.Status == EmployerRegistrationRequestStatus.Pending))
        {
            throw new ConflictException("Bạn đã gửi yêu cầu đăng ký nhà tuyển dụng và đang chờ xử lý.");
        }

        ValidateDateOfBirth(dto.DateOfBirth);
        ValidateUploadedFile(dto.DocumentFile, "Tài liệu xác thực doanh nghiệp",
            BusinessRuleConstants.FileUpload.AllowedDocumentExtensions);
        ValidateUploadedFile(dto.CCCDFrontImage, "Ảnh CCCD mặt trước",
            BusinessRuleConstants.FileUpload.AllowedImageExtensions);
        ValidateUploadedFile(dto.CCCDBackImage, "Ảnh CCCD mặt sau",
            BusinessRuleConstants.FileUpload.AllowedImageExtensions);

        var requestId = Guid.NewGuid();
        var basePath = $"employer-registrations/{Guid.NewGuid():N}";
        var uploadedKeys = new List<string>();
        string documentPath;
        string cccdFrontImagePath;
        string cccdBackImagePath;
        try
        {
            documentPath = await UploadFileAsync($"{basePath}/document", dto.DocumentFile!);
            uploadedKeys.Add(documentPath);
            cccdFrontImagePath = await UploadFileAsync($"{basePath}/cccd-front", dto.CCCDFrontImage!);
            uploadedKeys.Add(cccdFrontImagePath);
            cccdBackImagePath = await UploadFileAsync($"{basePath}/cccd-back", dto.CCCDBackImage!);
            uploadedKeys.Add(cccdBackImagePath);
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try
                {
                    await storageService.DeleteFileAsync(key);
                }
                catch
                {
                    // Bỏ qua lỗi dọn dẹp để không che lỗi gốc.
                }
            }

            throw;
        }

        var request = dto.ToEntity(
            requestId,
            userId,
            documentPath,
            cccdFrontImagePath,
            cccdBackImagePath,
            DateTime.UtcNow,
            EmployerRegistrationRequestStatus.Pending);

        dbContext.EmployerRegistrationRequests.Add(request);
        await dbContext.SaveChangesAsync();

        return request.ToResponseDto();
    }

    public async Task<List<EmployerRegistrationViewDto>> GetMyRegistrationsAsync(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();

        return await dbContext.EmployerRegistrationRequests
            .AsNoTracking()
            .Where(r => r.RequesterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToViewDto()
            .ToListAsync();
    }

    public async Task<EmployerRegistrationDetailDto> GetMyRegistrationByIdAsync(ClaimsPrincipal user, Guid id)
    {
        var userId = user.GetUserId();

        var request = await dbContext.EmployerRegistrationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

        if (request == null)
        {
            throw new NotFoundException("Không tìm thấy yêu cầu đăng ký.");
        }

        return request.ToDetailDto(
            await storageService.GeneratePresignedUrlAsync(request.DocumentPath,TimeSpan.FromMinutes(15)),
            await storageService.GeneratePresignedUrlAsync(request.CCCDFrontImagePath,TimeSpan.FromMinutes(15)),
            await storageService.GeneratePresignedUrlAsync(request.CCCDBackImagePath,TimeSpan.FromMinutes(15)));
    }

    public async Task CancelAsync(ClaimsPrincipal user, Guid id)
    {
        var userId = user.GetUserId();

        var request = await dbContext.EmployerRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

        if (request == null)
        {
            throw new NotFoundException("Không tìm thấy yêu cầu đăng ký.");
        }

        if (request.Status != EmployerRegistrationRequestStatus.Pending)
        {
            throw new ConflictException("Chỉ có thể huỷ yêu cầu đăng ký đang chờ xử lý.");
        }

        request.Status = EmployerRegistrationRequestStatus.Cancelled;
        await dbContext.SaveChangesAsync();
    }

    private async Task<string> UploadFileAsync(string keyPrefix, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var key = $"{keyPrefix}{extension.ToLowerInvariant()}";
        await using var stream = file.OpenReadStream();
        await storageService.UploadFileAsync(key, stream, file.ContentType);
        return key;
    }

    private static void ValidateDateOfBirth(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
        {
            throw new BadRequestException("Ngày sinh là bắt buộc.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dateOfBirth.Value > today)
        {
            throw new BadRequestException("Ngày sinh không được ở tương lai.");
        }

        if (dateOfBirth.Value < today.AddYears(-100))
        {
            throw new BadRequestException("Ngày sinh không hợp lệ.");
        }
    }

    private static void ValidateUploadedFile(IFormFile? file, string fieldLabel, string[] allowedExtensions)
    {
        if (file == null || file.Length == 0)
        {
            throw new BadRequestException($"{fieldLabel} là bắt buộc.");
        }

        if (file.Length > BusinessRuleConstants.FileUpload.MaxFileSizeInBytes)
        {
            throw new BadRequestException($"{fieldLabel} không được vượt quá 5MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"{fieldLabel} phải có định dạng {string.Join(", ", allowedExtensions)}.");
        }
    }
}

public class EmployerRegistrationRequestDto
{
    [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.CompanyNameMaxLength,
        ErrorMessage = "Tên công ty không được vượt quá 100 ký tự.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã số thuế là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.TaxCodeMaxLength,
        ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự.")]
    public string TaxCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.FullNameMaxLength,
        ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số CCCD là bắt buộc.")]
    [RegularExpression(BusinessRuleConstants.CCCDNumberRegex, ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.")]
    public string CCCDNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tài liệu xác thực doanh nghiệp là bắt buộc.")]
    public IFormFile? DocumentFile { get; set; }

    [Required(ErrorMessage = "Ảnh CCCD mặt trước là bắt buộc.")]
    public IFormFile? CCCDFrontImage { get; set; }

    [Required(ErrorMessage = "Ảnh CCCD mặt sau là bắt buộc.")]
    public IFormFile? CCCDBackImage { get; set; }
}

public class EmployerRegistrationResponseDto
{
    public required Guid Id { get; set; }
    public required string Status { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class EmployerRegistrationDetailDto
{
    public required Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public required string FullName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CCCDNumber { get; set; }
    public required string DocumentUrl { get; set; }
    public required string CCCDFrontImageUrl { get; set; }
    public required string CCCDBackImageUrl { get; set; }
    public required string Status { get; set; }
    public string? RejectReason { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class EmployerRegistrationViewDto
{
    public required Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public required string FullName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CCCDNumber { get; set; }
    public required string Status { get; set; }
    public string? RejectReason { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployerRegistrationMapper
{
    [MapperIgnoreTarget(nameof(EmployerRegistrationRequest.RejectReason))]
    [MapperIgnoreTarget(nameof(EmployerRegistrationRequest.ProcessedAt))]
    [MapperIgnoreTarget(nameof(EmployerRegistrationRequest.ProcessorId))]
    [MapperIgnoreTarget(nameof(EmployerRegistrationRequest.Requester))]
    [MapperIgnoreTarget(nameof(EmployerRegistrationRequest.Processor))]
    public static partial EmployerRegistrationRequest ToEntity(
        this EmployerRegistrationRequestDto dto,
        Guid id,
        Guid requesterId,
        string documentPath,
        string cccdFrontImagePath,
        string cccdBackImagePath,
        DateTime createdAt,
        EmployerRegistrationRequestStatus status);

    public static partial EmployerRegistrationResponseDto ToResponseDto(this EmployerRegistrationRequest request);

    public static partial EmployerRegistrationDetailDto ToDetailDto(
        this EmployerRegistrationRequest request,
        string documentUrl,
        string cccdFrontImageUrl,
        string cccdBackImageUrl);

    public static partial IQueryable<EmployerRegistrationViewDto> ProjectToViewDto(
        this IQueryable<EmployerRegistrationRequest> requests);

    [UserMapping(Default = true)]
    private static string Trim(string value) => value.Trim();
}
