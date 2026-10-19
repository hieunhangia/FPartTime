using System.Data;
using System.Security.Claims;
using Contracts.Application;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Extensions;
using Repository.Models.Jobs;
using Repository.Models.Users;
using Riok.Mapperly.Abstractions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices.Censor;

public class JobReportService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    INotificationsService notificationsService)
{
    public IQueryable<CensorJobReportViewDto> GetReportsQueryable(JobReportStatus? status = null)
    {
        var query = dbContext.JobReports.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToViewDto();
    }

    public async Task<CensorJobReportDetailDto> GetReportByIdAsync(Guid id)
    {
        var report = await dbContext.JobReports.AsNoTracking()
                         .Where(r => r.Id == id)
                         .ProjectToDetailDto()
                         .FirstOrDefaultAsync()
                     ?? throw new NotFoundException("Không tìm thấy đơn báo cáo.");

        return report;
    }

    public async Task LockJobAsync(ClaimsPrincipal user, Guid id)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await UpdateReportStatusAsync(user, id, JobReportStatus.Resolved);

        var job = await GetReportedJobAsync(id);
        job.Status = JobStatus.AdminHidden;

        await dbContext.SaveChangesAsync();
        var reporterIds = await ResolveRelatedReportsAsync(user, id,
            dbContext.JobReports.Where(r => r.JobId == job.Id));
        await notificationsService.CreateNotificationsAsync(reporterIds, "Kết quả báo cáo",
            $"Báo cáo được chấp nhận. Tin \"{job.Title}\" đã bị khóa.");
        await notificationsService.CreateNotificationAsync(job.EmployerId, "Kết quả báo cáo",
            $"Tin \"{job.Title}\" của bạn bị khóa do vi phạm.");
        await transaction.CommitAsync();
    }

    public async Task LockEmployerAsync(ClaimsPrincipal user, Guid id)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await UpdateReportStatusAsync(user, id, JobReportStatus.Resolved);

        var job = await GetReportedJobAsync(id);
        var employerUser = await userManager.FindByIdAsync(job.EmployerId.ToString())
                           ?? throw new NotFoundException("Không tìm thấy tài khoản nhà tuyển dụng.");

        employerUser.LockoutEnabled = true;
        employerUser.LockoutEnd = DateTimeOffset.MaxValue;
        var result = await userManager.UpdateAsync(employerUser);
        if (!result.Succeeded)
        {
            throw new InternalServerErrorException("Không thể khóa tài khoản nhà tuyển dụng.");
        }

        await dbContext.Jobs
            .Where(j => j.EmployerId == job.EmployerId && j.Status != JobStatus.AdminHidden)
            .ExecuteUpdateAsync(setters => setters.SetProperty(j => j.Status, JobStatus.AdminHidden));

        var reporterIds = await ResolveRelatedReportsAsync(user, id,
            dbContext.JobReports.Where(r => r.Job!.EmployerId == job.EmployerId));
        await notificationsService.CreateNotificationsAsync(reporterIds, "Kết quả báo cáo",
            "Báo cáo được chấp nhận. Tài khoản nhà tuyển dụng đã bị khóa");
        await notificationsService.CreateNotificationAsync(job.EmployerId, "Kết quả báo cáo",
            "Tài khoản của bạn bị khóa vô thời hạn");
        await transaction.CommitAsync();
    }

    private static async Task<List<Guid>> ResolveRelatedReportsAsync(
        ClaimsPrincipal user, Guid selectedReportId, IQueryable<JobReport> reports)
    {
        var censorId = user.GetUserId();
        var reporterIds = await reports
            .Where(r => r.Status == JobReportStatus.Pending || r.Id == selectedReportId)
            .Select(r => r.ReporterId)
            .Distinct()
            .ToListAsync();

        await reports.Where(r => r.Status == JobReportStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, JobReportStatus.Resolved)
                .SetProperty(r => r.ResolverId, censorId));

        return reporterIds;
    }

    public async Task RejectAsync(ClaimsPrincipal user, Guid id)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await UpdateReportStatusAsync(user, id, JobReportStatus.Dismissed);

        var job = await GetReportedJobAsync(id);
        await NotifyReportResultAsync(id, job.EmployerId,
            $"Báo cáo của bạn về tin \"{job.Title}\" bị từ chối.",
            $"Báo cáo về tin \"{job.Title}\" của bạn đã bị từ chối.");
        await transaction.CommitAsync();
    }

    private async Task NotifyReportResultAsync(Guid reportId, Guid employerId, string reporterContent,
        string employerContent)
    {
        var reporterId = await dbContext.JobReports
            .Where(r => r.Id == reportId)
            .Select(r => r.ReporterId)
            .FirstAsync();

        const string title = "Kết quả báo cáo";
        await notificationsService.CreateNotificationAsync(reporterId, title, reporterContent);
        await notificationsService.CreateNotificationAsync(employerId, title, employerContent);
    }

    private async Task<Job> GetReportedJobAsync(Guid reportId) =>
        await dbContext.JobReports
            .Where(r => r.Id == reportId)
            .Select(r => r.Job)
            .FirstOrDefaultAsync()
        ?? throw new NotFoundException("Không tìm thấy tin tuyển dụng bị báo cáo.");

    private async Task UpdateReportStatusAsync(ClaimsPrincipal user, Guid id, JobReportStatus status)
    {
        var censorId = user.GetUserId();
        var updatedCount = await dbContext.JobReports
            .Where(r => r.Id == id && r.Status == JobReportStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.ResolverId, censorId));

        if (updatedCount == 0)
        {
            if (!await dbContext.JobReports.AnyAsync(r => r.Id == id))
            {
                throw new NotFoundException("Không tìm thấy đơn báo cáo.");
            }

            throw new ConflictException("Chỉ có thể xử lý đơn báo cáo đang chờ xử lý.");
        }
    }
}

public class CensorJobReportViewDto
{
    public required Guid Id { get; set; }
    public required Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployerCompanyName { get; set; }
    public required Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterPhoneNumber { get; set; }
    public required string Reason { get; set; }
    public required JobReportStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public Guid? ResolverId { get; set; }
    public string? ResolverName { get; set; }
}

public class CensorJobReportDetailDto
{
    public required Guid Id { get; set; }
    public required string Reason { get; set; }
    public required JobReportStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }

    // Thông tin công việc bị báo cáo
    public required Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public string? JobDescription { get; set; }
    public JobStatus? JobStatus { get; set; }
    public DateOnly? JobExpiryAt { get; set; }
    public DateTime? JobCreatedAt { get; set; }
    public string? JobDetailAddress { get; set; }
    public string? JobCommuneName { get; set; }
    public string? JobProvinceName { get; set; }

    // Thông tin nhà tuyển dụng của công việc
    public Guid? EmployerId { get; set; }
    public string? EmployerCompanyName { get; set; }
    public string? EmployerContactPhone { get; set; }

    // Thông tin ứng viên gửi báo cáo
    public required Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterPhoneNumber { get; set; }
    public string? ReporterAvatarFilePath { get; set; }

    // Thông tin kiểm duyệt viên xử lý (nếu có)
    public Guid? ResolverId { get; set; }
    public string? ResolverName { get; set; }
    public string? ResolverEmail { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class CensorJobReportMapper
{
    public static partial IQueryable<CensorJobReportViewDto> ProjectToViewDto(this IQueryable<JobReport> reports);

    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Title)}", nameof(CensorJobReportViewDto.JobTitle))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Employer)}.{nameof(JobReport.Job.Employer.CompanyName)}",
        nameof(CensorJobReportViewDto.EmployerCompanyName))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.FullName)}", nameof(CensorJobReportViewDto.ReporterName))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.User)}.{nameof(JobReport.Reporter.User.Email)}",
        nameof(CensorJobReportViewDto.ReporterEmail))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.User)}.{nameof(JobReport.Reporter.User.PhoneNumber)}",
        nameof(CensorJobReportViewDto.ReporterPhoneNumber))]
    [MapProperty($"{nameof(JobReport.Resolver)}.{nameof(JobReport.Resolver.User)}.{nameof(JobReport.Resolver.User.UserName)}",
        nameof(CensorJobReportViewDto.ResolverName))]
    public static partial CensorJobReportViewDto MapToViewDto(this JobReport report);

    public static partial IQueryable<CensorJobReportDetailDto> ProjectToDetailDto(this IQueryable<JobReport> reports);

    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Title)}", nameof(CensorJobReportDetailDto.JobTitle))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Description)}", nameof(CensorJobReportDetailDto.JobDescription))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Status)}", nameof(CensorJobReportDetailDto.JobStatus))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.ExpiryAt)}", nameof(CensorJobReportDetailDto.JobExpiryAt))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.CreatedAt)}", nameof(CensorJobReportDetailDto.JobCreatedAt))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.DetailAddress)}", nameof(CensorJobReportDetailDto.JobDetailAddress))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Commune)}.{nameof(JobReport.Job.Commune.Name)}",
        nameof(CensorJobReportDetailDto.JobCommuneName))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Commune)}.{nameof(JobReport.Job.Commune.Province)}.{nameof(JobReport.Job.Commune.Province.Name)}",
        nameof(CensorJobReportDetailDto.JobProvinceName))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.EmployerId)}", nameof(CensorJobReportDetailDto.EmployerId))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Employer)}.{nameof(JobReport.Job.Employer.CompanyName)}",
        nameof(CensorJobReportDetailDto.EmployerCompanyName))]
    [MapProperty($"{nameof(JobReport.Job)}.{nameof(JobReport.Job.Employer)}.{nameof(JobReport.Job.Employer.ContactPhone)}",
        nameof(CensorJobReportDetailDto.EmployerContactPhone))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.FullName)}", nameof(CensorJobReportDetailDto.ReporterName))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.User)}.{nameof(JobReport.Reporter.User.Email)}",
        nameof(CensorJobReportDetailDto.ReporterEmail))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.User)}.{nameof(JobReport.Reporter.User.PhoneNumber)}",
        nameof(CensorJobReportDetailDto.ReporterPhoneNumber))]
    [MapProperty($"{nameof(JobReport.Reporter)}.{nameof(JobReport.Reporter.AvatarFilePath)}",
        nameof(CensorJobReportDetailDto.ReporterAvatarFilePath))]
    [MapProperty($"{nameof(JobReport.Resolver)}.{nameof(JobReport.Resolver.User)}.{nameof(JobReport.Resolver.User.UserName)}",
        nameof(CensorJobReportDetailDto.ResolverName))]
    [MapProperty($"{nameof(JobReport.Resolver)}.{nameof(JobReport.Resolver.User)}.{nameof(JobReport.Resolver.User.Email)}",
        nameof(CensorJobReportDetailDto.ResolverEmail))]
    public static partial CensorJobReportDetailDto MapToDetailDto(this JobReport report);
}
