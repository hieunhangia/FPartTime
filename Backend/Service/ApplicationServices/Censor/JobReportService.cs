using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Models.Jobs;
using Riok.Mapperly.Abstractions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices.Censor;

public class JobReportService(ApplicationDbContext dbContext)
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

    [MapProperty("Job.Title", nameof(CensorJobReportViewDto.JobTitle))]
    [MapProperty("Job.Employer.CompanyName", nameof(CensorJobReportViewDto.EmployerCompanyName))]
    [MapProperty("Reporter.FullName", nameof(CensorJobReportViewDto.ReporterName))]
    [MapProperty("Reporter.User.Email", nameof(CensorJobReportViewDto.ReporterEmail))]
    [MapProperty("Reporter.User.PhoneNumber", nameof(CensorJobReportViewDto.ReporterPhoneNumber))]
    [MapProperty("Resolver.User.UserName", nameof(CensorJobReportViewDto.ResolverName))]
    public static partial CensorJobReportViewDto MapToViewDto(this JobReport report);

    public static partial IQueryable<CensorJobReportDetailDto> ProjectToDetailDto(this IQueryable<JobReport> reports);

    [MapProperty("Job.Title", nameof(CensorJobReportDetailDto.JobTitle))]
    [MapProperty("Job.Description", nameof(CensorJobReportDetailDto.JobDescription))]
    [MapProperty("Job.Status", nameof(CensorJobReportDetailDto.JobStatus))]
    [MapProperty("Job.ExpiryAt", nameof(CensorJobReportDetailDto.JobExpiryAt))]
    [MapProperty("Job.CreatedAt", nameof(CensorJobReportDetailDto.JobCreatedAt))]
    [MapProperty("Job.DetailAddress", nameof(CensorJobReportDetailDto.JobDetailAddress))]
    [MapProperty("Job.Commune.Name", nameof(CensorJobReportDetailDto.JobCommuneName))]
    [MapProperty("Job.Commune.Province.Name", nameof(CensorJobReportDetailDto.JobProvinceName))]
    [MapProperty("Job.EmployerId", nameof(CensorJobReportDetailDto.EmployerId))]
    [MapProperty("Job.Employer.CompanyName", nameof(CensorJobReportDetailDto.EmployerCompanyName))]
    [MapProperty("Job.Employer.ContactPhone", nameof(CensorJobReportDetailDto.EmployerContactPhone))]
    [MapProperty("Reporter.FullName", nameof(CensorJobReportDetailDto.ReporterName))]
    [MapProperty("Reporter.User.Email", nameof(CensorJobReportDetailDto.ReporterEmail))]
    [MapProperty("Reporter.User.PhoneNumber", nameof(CensorJobReportDetailDto.ReporterPhoneNumber))]
    [MapProperty("Reporter.AvatarFilePath", nameof(CensorJobReportDetailDto.ReporterAvatarFilePath))]
    [MapProperty("Resolver.User.UserName", nameof(CensorJobReportDetailDto.ResolverName))]
    [MapProperty("Resolver.User.Email", nameof(CensorJobReportDetailDto.ResolverEmail))]
    public static partial CensorJobReportDetailDto MapToDetailDto(this JobReport report);
}

