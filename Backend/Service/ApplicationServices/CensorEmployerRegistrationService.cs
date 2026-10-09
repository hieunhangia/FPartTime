using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Users;
using Repository.Constants;
using Service.ExternalServices;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices;

public class RejectEmployerRegistrationRequestDto
{
    [Required(ErrorMessage = "Lý do từ chối là bắt buộc.")]
    [MaxLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự.")]
    public string RejectReason { get; set; } = string.Empty;
}

public class CensorEmployerRegistrationService(
    ApplicationDbContext dbContext,
    CloudflareR2StorageService storageService,
    UserManager<User> userManager)
{
    public async Task<List<EmployerRegistrationViewDto>> GetRegistrationsAsync(EmployerRegistrationRequestStatus? status)
    {
        var query = dbContext.EmployerRegistrationRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToViewDto()
            .ToListAsync();
    }

    public async Task<EmployerRegistrationDetailDto> GetRegistrationByIdAsync(Guid id)
    {
        var request = await dbContext.EmployerRegistrationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id) ?? throw new NotFoundException("Không tìm thấy yêu cầu đăng ký.");

        return request.ToDetailDto(
            await storageService.GeneratePresignedUrlAsync(request.DocumentPath, TimeSpan.FromMinutes(15)),
            await storageService.GeneratePresignedUrlAsync(request.CCCDFrontImagePath, TimeSpan.FromMinutes(15)),
            await storageService.GeneratePresignedUrlAsync(request.CCCDBackImagePath, TimeSpan.FromMinutes(15)));
    }

    public async Task ApproveAsync(ClaimsPrincipal user, Guid id)
    {
        var censorId = user.GetUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var request = await dbContext.EmployerRegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == id) ?? throw new NotFoundException("Không tìm thấy yêu cầu đăng ký.");

            if (request.Status != EmployerRegistrationRequestStatus.Pending)
                throw new ConflictException("Chỉ có thể duyệt yêu cầu đang chờ xử lý.");

            var requesterUser = await userManager.FindByIdAsync(request.RequesterId.ToString()) ?? throw new NotFoundException("Người dùng không tồn tại.");

            request.Status = EmployerRegistrationRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessorId = censorId;

            var employerProfile = new EmployerProfile
            {
                UserId = request.RequesterId,
                CompanyName = request.CompanyName,
                TaxCode = request.TaxCode,
                VerificationDocumentFilePath = request.DocumentPath,
                MaxActivePosts = 5,
                ContactPhone = request.PhoneNumber
            };
            
            if (!await dbContext.Employers.AnyAsync(e => e.UserId == request.RequesterId))
            {
                dbContext.Employers.Add(employerProfile);
            }

            if (!await userManager.IsInRoleAsync(requesterUser, Role.Employer))
            {
                var roleResult = await userManager.AddToRoleAsync(requesterUser, Role.Employer);
                if (!roleResult.Succeeded)
                {
                    throw new Exception("Lỗi khi thêm quyền Employer cho người dùng.");
                }
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RejectAsync(ClaimsPrincipal user, Guid id, string rejectReason)
    {
        var censorId = user.GetUserId();

        var request = await dbContext.EmployerRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new NotFoundException("Không tìm thấy yêu cầu đăng ký.");

        if (request.Status != EmployerRegistrationRequestStatus.Pending)
            throw new ConflictException("Chỉ có thể từ chối yêu cầu đang chờ xử lý.");

        request.Status = EmployerRegistrationRequestStatus.Rejected;
        request.RejectReason = rejectReason;
        request.ProcessedAt = DateTime.UtcNow;
        request.ProcessorId = censorId;

        await dbContext.SaveChangesAsync();
    }
}

