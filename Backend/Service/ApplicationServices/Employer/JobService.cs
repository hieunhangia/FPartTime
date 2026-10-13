using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Jobs;
using Riok.Mapperly.Abstractions;
using Service.Extensions;
using Service.HttpErrorExceptions;

namespace Service.ApplicationServices.Employer;

public class JobService(ApplicationDbContext context)
{
    public IQueryable<JobSummaryResponseDto> GetMyCreatedJobsQueryable(ClaimsPrincipal user) =>
        context.Jobs.AsNoTracking()
            .Where(j => j.EmployerId == user.GetUserId())
            .ProjectToSummaryDto();

    public async Task<JobDetailResponseDto?> GetJobDetailAsync(ClaimsPrincipal user, Guid jobId) =>
        await context.Jobs.AsNoTracking()
            .Where(j => j.Id == jobId && j.EmployerId == user.GetUserId())
            .ProjectToDetailDto()
            .FirstOrDefaultAsync();

    public async Task<JobApplicationDetailResponseDto?> GetJobApplicationDetailAsync(ClaimsPrincipal user,
        Guid jobApplicationId) => await context.JobApplications.AsNoTracking()
        .Where(ja => ja.Id == jobApplicationId && ja.Job!.EmployerId == user.GetUserId())
        .ProjectToDetailDto()
        .FirstOrDefaultAsync();

    public async Task<JobDetailResponseDto> CreateJobAsync(ClaimsPrincipal user,
        CreateJobRequestDto createJobRequestDto)
    {
        var employer = await context.Employers.FirstOrDefaultAsync(e => e.UserId == user.GetUserId());
        if (employer == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        if (employer.MaxActivePosts <=
            await context.Jobs.CountAsync(j => j.EmployerId == user.GetUserId() && j.Status == JobStatus.Active))
        {
            throw new BadRequestException(
                $"Bạn đã đạt giới hạn số lượng bài đăng công việc đang hoạt động ({employer.MaxActivePosts}). Vui lòng hủy một bài đăng công việc đang hoạt động trước khi tạo bài đăng mới.");
        }

        if (createJobRequestDto is { MinAgeRequirement: not null, MaxAgeRequirement: not null } &&
            createJobRequestDto.MinAgeRequirement.Value > createJobRequestDto.MaxAgeRequirement.Value)
        {
            throw new BadRequestException("Tuổi tối thiểu không được lớn hơn tuổi tối đa.");
        }

        if (createJobRequestDto.ExpiryAt.HasValue &&
            createJobRequestDto.ExpiryAt.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("Ngày hết hạn phải lớn hơn ngày hiện tại.");
        }

        foreach (var group in createJobRequestDto.JobSchedules.GroupBy(s => s.DayOfWeek))
        {
            var orderedSchedules = group.OrderBy(s => s.StartTime).ToList();
            for (var i = 0; i < orderedSchedules.Count; i++)
            {
                var cur = orderedSchedules[i];
                if (cur.StartTime >= cur.EndTime)
                {
                    throw new BadRequestException(
                        $"Ca làm việc ngày {GetVietnameseDayOfWeek(cur.DayOfWeek)} có thời gian kết thúc phải lớn hơn thời gian bắt đầu. Nếu ca làm việc kéo dài qua nửa đêm, vui lòng chia thành 2 ca làm việc riêng biệt.");
                }

                if ((cur.EndTime - cur.StartTime) < TimeSpan.FromMinutes(30))
                {
                    throw new BadRequestException(
                        $"Ca làm việc ngày {GetVietnameseDayOfWeek(cur.DayOfWeek)} phải kéo dài tối thiểu 30 phút.");
                }


                if (i <= 0) continue;

                var prev = orderedSchedules[i - 1];
                if (cur.StartTime < prev.EndTime)
                {
                    throw new BadRequestException(
                        $@"Các ca làm việc trong ngày {GetVietnameseDayOfWeek(cur.DayOfWeek)} bị trùng hoặc chồng chéo thời gian ({prev.StartTime:HH\:mm}-{prev.EndTime:HH\:mm} và {cur.StartTime:HH\:mm}-{cur.EndTime:HH\:mm}).");
                }
            }
        }

        if (createJobRequestDto.JobSalaries.GroupBy(s => new { s.TimeUnit, s.TimeQuantity }).Any(g => g.Count() > 1))
        {
            throw new BadRequestException("Không được thiết lập trùng lặp đơn vị tính lương và thời gian.");
        }

        foreach (var salary in createJobRequestDto.JobSalaries)
        {
            if (salary.Amount % 1 != 0)
            {
                throw new BadRequestException($"Số tiền lương ({salary.Amount:N0}) không được chứa số lẻ thập phân.");
            }

            switch (salary)
            {
                case { TimeUnit: JobSalaryTimeUnit.Hour, TimeQuantity: > 24 }:
                    throw new BadRequestException("Số giờ cho một định mức lương không được vượt quá 24 giờ.");
                case { TimeUnit: JobSalaryTimeUnit.Day, TimeQuantity: > 31 }:
                    throw new BadRequestException("Số ngày cho một định mức lương không được vượt quá 31 ngày.");
                case { TimeUnit: JobSalaryTimeUnit.Month, TimeQuantity: > 12 }:
                    throw new BadRequestException("Số tháng cho một định mức lương không được vượt quá 12 tháng.");
                case { TimeUnit: JobSalaryTimeUnit.Week, TimeQuantity: > 52 }:
                    throw new BadRequestException("Số tuần cho một định mức lương không được vượt quá 52 tuần.");
                case { TimeUnit: JobSalaryTimeUnit.Session, TimeQuantity: > 2 }:
                    throw new BadRequestException("Số buổi cho một định mức lương không được vượt quá 2 buổi.");
                case { TimeUnit: JobSalaryTimeUnit.Project, TimeQuantity: > 1 }:
                    throw new BadRequestException("Số dự án cho một định mức lương không được vượt quá 1 dự án.");
            }
        }

        if (!await context.Industries.AnyAsync(i => i.Id == createJobRequestDto.IndustryId))
        {
            throw new BadRequestException("Ngành nghề không tồn tại.");
        }

        if (!await context.Communes.AnyAsync(c => c.Code == createJobRequestDto.CommuneCode))
        {
            throw new BadRequestException("Xã/Phường không hợp lệ hoặc không tồn tại.");
        }

        var job = context.Jobs.Add(createJobRequestDto.MapToEntity(user.GetUserId()));
        await context.SaveChangesAsync();
        return await GetJobDetailAsync(user, job.Entity.Id) ??
               throw new NotFoundException("Không tìm thấy công việc vừa tạo.");

        string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => dayOfWeek.ToString()
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class JobMapper
{
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Name)}", nameof(JobSummaryResponseDto.CommuneName))]
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Province)}.{nameof(Job.Commune.Province.Name)}",
        nameof(JobSummaryResponseDto.ProvinceName))]
    private static partial JobSummaryResponseDto MapToSummaryDto(Job job);

    public static partial IQueryable<JobSummaryResponseDto> ProjectToSummaryDto(this IQueryable<Job> job);

    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Name)}", nameof(JobDetailResponseDto.CommuneName))]
    [MapProperty($"{nameof(Job.Commune)}.{nameof(Job.Commune.Province)}.{nameof(Job.Commune.Province.Name)}",
        nameof(JobDetailResponseDto.ProvinceName))]
    public static partial JobDetailResponseDto MapToDetailDto(this Job job);

    [MapProperty($"{nameof(JobApplication.Candidate)}.{nameof(JobApplication.Candidate.FullName)}",
        nameof(JobApplicationSummaryResponseDto.CandidateFullName))]
    [MapProperty(
        $"{nameof(JobApplication.Candidate)}.{nameof(JobApplication.Candidate.User)}.{nameof(JobApplication.Candidate.User.PhoneNumber)}",
        nameof(JobApplicationSummaryResponseDto.CandidatePhoneNumber))]
    private static partial JobApplicationSummaryResponseDto MapToSummaryDto(JobApplication jobApplication);

    public static partial IQueryable<JobDetailResponseDto> ProjectToDetailDto(this IQueryable<Job> job);

    [MapProperty($"{nameof(JobApplication.Candidate)}.{nameof(JobApplication.Candidate.AvatarFilePath)}",
        nameof(JobApplicationDetailResponseDto.CandidateAvatarUrl))]
    [MapProperty(
        $"{nameof(JobApplication.Candidate)}.{nameof(JobApplication.Candidate.PreferredWorkCommune)}.{nameof(JobApplication.Candidate.PreferredWorkCommune.Province)}.{nameof(JobApplication.Candidate.PreferredWorkCommune.Province.Name)}",
        nameof(JobApplicationDetailResponseDto.CandidatePreferredWorkProvinceName))]
    [MapProperty(
        $"{nameof(JobApplication.Candidate)}.{nameof(JobApplication.Candidate.User)}.{nameof(JobApplication.Candidate.User.PhoneNumber)}",
        nameof(JobApplicationDetailResponseDto.CandidatePhoneNumber))]
    private static partial JobApplicationDetailResponseDto MapToDetailDto(JobApplication jobApplication);

    public static partial IQueryable<JobApplicationDetailResponseDto> ProjectToDetailDto(
        this IQueryable<JobApplication> jobApplication);

    public static partial Job MapToEntity(this CreateJobRequestDto createJobRequestDto, Guid employerId);
}

public class JobSummaryResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string IndustryName { get; set; }
    public required string DetailAddress { get; set; }
    public required string CommuneName { get; set; }
    public required string ProvinceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public required JobStatus Status { get; set; }
    public required List<JobSalaryResponseDto> JobSalaries { get; set; }
}

public class JobDetailResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int? VacancyCount { get; set; }
    public Gender? GenderRequirement { get; set; }
    public int? MinAgeRequirement { get; set; }
    public int? MaxAgeRequirement { get; set; }
    public required string DetailAddress { get; set; }
    public required string CommuneName { get; set; }
    public required string ProvinceName { get; set; }
    public DateOnly? ExpiryAt { get; set; }
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string IndustryName { get; set; }
    public required List<JobScheduleResponseDto> JobSchedules { get; set; }
    public required List<JobSalaryResponseDto> JobSalaries { get; set; }
    public required List<JobApplicationSummaryResponseDto> JobApplications { get; set; }
}

public class JobApplicationDetailResponseDto
{
    public required Guid Id { get; set; }
    public required string CandidateAvatarUrl { get; set; }
    public required string CandidateBio { get; set; }
    public required string CandidateFullName { get; set; }
    public required string CandidatePhoneNumber { get; set; }
    public required DateOnly? CandidateDateOfBirth { get; set; }
    public required Gender? CandidateGender { get; set; }
    public required string CandidatePreferredWorkCommuneName { get; set; }
    public required string CandidatePreferredWorkProvinceName { get; set; }
    public required List<JobScheduleResponseDto> CandidatePreferredWorkSchedules { get; set; }
    public required JobApplicationStatus Status { get; set; }
    public required DateTime AppliedAt { get; set; }
}

public class CreateJobRequestDto
{
    [Required(ErrorMessage = "Tiêu đề công việc là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.Job.TitleMaxLength,
        ErrorMessage = "Tiêu đề công việc không được vượt quá {1} ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả công việc là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.Job.DescriptionMaxLength,
        ErrorMessage = "Mô tả công việc không được vượt quá {1} ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, BusinessRuleConstants.Jobs.MaxVacanciesPerJob,
        ErrorMessage = "Số lượng tuyển dụng phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng {1}.")]
    public int? VacancyCount { get; set; }

    public Gender? GenderRequirement { get; set; }

    [Range(15, 100, ErrorMessage = "Tuổi tối thiểu phải lớn hơn hoặc bằng 15 và nhỏ hơn hoặc bằng 100.")]
    public int? MinAgeRequirement { get; set; }

    [Range(15, 100, ErrorMessage = "Tuổi tối đa phải lớn hơn hoặc bằng 15 và nhỏ hơn hoặc bằng 100.")]
    public int? MaxAgeRequirement { get; set; }

    public DateOnly? ExpiryAt { get; set; }

    [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.Job.DetailAddressMaxLength,
        ErrorMessage = "Địa chỉ chi tiết không được vượt quá {1} ký tự.")]
    public string DetailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xã/Phường là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.Commune.CodeMaxLength,
        ErrorMessage = "Mã xã/phường không được vượt quá {1} ký tự.")]
    public string CommuneCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngành nghề là bắt buộc.")]
    public Guid IndustryId { get; set; }

    [Required(ErrorMessage = "Lịch làm việc là bắt buộc.")]
    [Length(1, BusinessRuleConstants.Jobs.MaxJobSchedulesPerJob,
        ErrorMessage = "Số lượng lịch làm việc phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng {1}.")]
    public List<JobScheduleRequestDto> JobSchedules { get; set; } = [];

    [Required(ErrorMessage = "Lương là bắt buộc.")]
    [Length(1, BusinessRuleConstants.Jobs.MaxJobSalariesPerJob,
        ErrorMessage = "Số lượng lương phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng {1}.")]
    public List<JobSalaryRequestDto> JobSalaries { get; set; } = [];
}

public class JobScheduleRequestDto
{
    [Required(ErrorMessage = "Ngày trong tuần là bắt buộc.")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
    public TimeOnly EndTime { get; set; }
}

public class JobSalaryRequestDto
{
    [Required(ErrorMessage = "Số tiền là bắt buộc.")]
    [Range(1_000, 500_000_000, ErrorMessage = "Số tiền phải lớn hơn 1,000 và nhỏ hơn hoặc bằng 500,000,000.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Số lượng thời gian là bắt buộc.")]
    [Range(1, 1000, ErrorMessage = "Số lượng thời gian phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng 1000.")]
    public int TimeQuantity { get; set; }

    [Required(ErrorMessage = "Đơn vị thời gian là bắt buộc.")]
    public JobSalaryTimeUnit TimeUnit { get; set; }
}

public class JobScheduleResponseDto
{
    public required Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
}

public class JobSalaryResponseDto
{
    public required decimal Amount { get; set; }
    public required int TimeQuantity { get; set; }
    public required JobSalaryTimeUnit TimeUnit { get; set; }
}

public class JobApplicationSummaryResponseDto
{
    public required Guid Id { get; set; }
    public required string CandidateFullName { get; set; }
    public required string CandidatePhoneNumber { get; set; }
    public required JobApplicationStatus Status { get; set; }
    public required DateTime AppliedAt { get; set; }
}