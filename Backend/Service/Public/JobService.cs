using Domain;
using Domain.Models.Jobs;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Service.Public;

public class JobService(ApplicationDbContext dbContext)
{
    public IQueryable<JobResponseDto> GetActiveJobsQueryable()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dbContext.Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Active && j.ExpiryAt >= today)
            .OrderByDescending(j => j.LastBumpedAt)
            .ThenBy(j => j.Id)
            .ProjectToDto();
    }

    public async Task<JobResponseDto> GetActiveJobByIdAsync(Guid id)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var job = await dbContext.Jobs.AsNoTracking()
            .Where(j => j.Id == id && j.Status == JobStatus.Active && j.ExpiryAt >= today)
            .ProjectToDto()
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Không tìm thấy công việc hoặc công việc không còn khả dụng.");
        return job;
    }
}

public class JobResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int? VacancyCount { get; set; }
    public Gender? GenderRequirement { get; set; }
    public int? MinAgeRequirement { get; set; }
    public int? MaxAgeRequirement { get; set; }
    public required string CommuneCode { get; set; }
    public string? CommuneName { get; set; }
    public string? ProvinceCode { get; set; }
    public string? ProvinceName { get; set; }
    public required string DetailAddress { get; set; }
    public required DateOnly ExpiryAt { get; set; }
    public required JobStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public required Guid EmployerId { get; set; }
    public string? EmployerCompanyName { get; set; }
    public string? EmployerLogoFilePath { get; set; }
    public string? EmployerContactPhone { get; set; }
    public string? EmployerDescription { get; set; }

    public required Guid IndustryId { get; set; }
    public string? IndustryName { get; set; }

    public List<JobSalaryResponseDto> Salaries { get; set; } = [];
    public List<JobScheduleResponseDto> Schedules { get; set; } = [];
}

public class JobSalaryResponseDto
{
    public required Guid Id { get; set; }
    public required decimal Amount { get; set; }
    public required int TimeQuantity { get; set; }
    public required JobSalaryTimeUnit TimeUnit { get; set; }
}

public class JobScheduleResponseDto
{
    public required Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class JobMapper
{
    public static partial IQueryable<JobResponseDto> ProjectToDto(this IQueryable<Job> jobs);
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Name)}", nameof(JobResponseDto.CommuneName))]
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.ProvinceCode)}", nameof(JobResponseDto.ProvinceCode))]
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Province)}.{nameof(Job.Commune.Province.Name)}", nameof(JobResponseDto.ProvinceName))]
    [MapProperty($"{nameof(Job.Employer)}.{nameof(Job.Employer.CompanyName)}", nameof(JobResponseDto.EmployerCompanyName))]
    [MapProperty($"{nameof(Job.Employer)}.{nameof(Job.Employer.LogoFilePath)}", nameof(JobResponseDto.EmployerLogoFilePath))]
    [MapProperty($"{nameof(Job.Employer)}.{nameof(Job.Employer.ContactPhone)}", nameof(JobResponseDto.EmployerContactPhone))]
    [MapProperty($"{nameof(Job.Employer)}.{nameof(Job.Employer.Description)}", nameof(JobResponseDto.EmployerDescription))]
    [MapProperty($"{nameof(Job.Industry)}.{nameof(Job.Industry.Name)}", nameof(JobResponseDto.IndustryName))]
    [MapProperty(nameof(Job.JobSalaries), nameof(JobResponseDto.Salaries))]
    [MapProperty(nameof(Job.JobSchedules), nameof(JobResponseDto.Schedules))]
    public static partial JobResponseDto MapToDto(this Job job);

    public static partial JobSalaryResponseDto MapSalaryToDto(JobSalary salary);
    public static partial JobScheduleResponseDto MapScheduleToDto(JobSchedule schedule);
}
