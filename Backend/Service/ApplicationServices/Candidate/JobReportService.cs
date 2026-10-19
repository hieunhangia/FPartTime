using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Extensions;
using Repository.Models.Jobs;
using Repository.Models.Users;
using Riok.Mapperly.Abstractions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices.Candidate;

public class JobReportService(ApplicationDbContext dbContext)
{
    public async Task<JobReportResponseDto> CreateReportAsync(ClaimsPrincipal user, CreateJobReportDto dto)
    {
        var userId = user.GetUserId();

        if (!dto.JobId.HasValue)
        {
            throw new BadRequestException("Mã tin tuyển dụng không được để trống.");
        }

        var job = await dbContext.Jobs
                      .AsNoTracking()
                      .FirstOrDefaultAsync(j => j.Id == dto.JobId.Value)
                  ?? throw new NotFoundException("Không tìm thấy tin tuyển dụng.");

        if (job.EmployerId == userId)
        {
            throw new BadRequestException("Bạn không thể báo cáo tin tuyển dụng của chính mình.");
        }

        var candidateExists = await dbContext.Candidates.AnyAsync(c => c.UserId == userId);
        if (!candidateExists)
        {
            dbContext.Candidates.Add(new CandidateProfile { UserId = userId });
            await dbContext.SaveChangesAsync();
        }

        var hasPendingReport = await dbContext.JobReports.AnyAsync(r =>
            r.JobId == dto.JobId.Value &&
            r.ReporterId == userId &&
            r.Status == JobReportStatus.Pending);

        if (hasPendingReport)
        {
            throw new ConflictException("Bạn đã gửi báo cáo cho tin tuyển dụng này và đang chờ xử lý.");
        }

        var trimmedReason = dto.Reason.Trim();
        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            throw new BadRequestException("Lý do báo cáo không được để trống.");
        }

        var report = new JobReport
        {
            Id = Guid.NewGuid(),
            JobId = dto.JobId.Value,
            ReporterId = userId,
            Reason = trimmedReason,
            Status = JobReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.JobReports.Add(report);
        await dbContext.SaveChangesAsync();

        return new JobReportResponseDto
        {
            Id = report.Id,
            JobId = report.JobId,
            JobTitle = job.Title,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };
    }

    public IQueryable<JobReportResponseDto> GetMyReportsQueryable(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        return dbContext.JobReports.AsNoTracking()
            .Where(r => r.ReporterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToDto();
    }

    public async Task<JobReportResponseDto> GetMyReportByIdAsync(ClaimsPrincipal user, Guid id)
    {
        var userId = user.GetUserId();
        var report = await dbContext.JobReports.AsNoTracking()
                         .Where(r => r.Id == id && r.ReporterId == userId)
                         .ProjectToDto()
                         .FirstOrDefaultAsync()
                     ?? throw new NotFoundException("Không tìm thấy báo cáo hoặc bạn không có quyền truy cập.");

        return report;
    }
}

public class CreateJobReportDto
{
    [Required(ErrorMessage = "Mã tin tuyển dụng không được để trống.")]
    public Guid? JobId { get; set; }

    [Required(ErrorMessage = "Lý do báo cáo không được để trống.")]
    [MaxLength(BusinessRuleConstants.Models.JobReport.ReasonMaxLength,
        ErrorMessage = "Lý do báo cáo không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class ReportJobReasonDto
{
    [Required(ErrorMessage = "Lý do báo cáo không được để trống.")]
    [MaxLength(BusinessRuleConstants.Models.JobReport.ReasonMaxLength,
        ErrorMessage = "Lý do báo cáo không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class JobReportResponseDto
{
    public required Guid Id { get; set; }
    public required Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public required string Reason { get; set; }
    public required JobReportStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class JobReportMapper
{
    public static partial IQueryable<JobReportResponseDto> ProjectToDto(this IQueryable<JobReport> reports);

    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Title)}", nameof(JobReportResponseDto.JobTitle))]
    public static partial JobReportResponseDto MapToDto(this JobReport report);
}
