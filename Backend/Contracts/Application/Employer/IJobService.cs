using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Domain.Shared.Constants;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface IJobService
{
    IQueryable<JobSummaryResponseDto> GetMyCreatedJobsQueryable(ClaimsPrincipal user);
    Task<JobDetailResponseDto> GetJobDetailAsync(ClaimsPrincipal user, Guid jobId);
    Task<PostsQuotaResponseDto> GetPostsQuotaAsync(ClaimsPrincipal user);

    Task<JobDetailResponseDto>
        CreateJobAsync(ClaimsPrincipal user, CreateUpdateJobRequestDto createUpdateJobRequestDto);

    Task<CanUpdateJobResponseDto> CanUpdateJobAsync(ClaimsPrincipal user, Guid jobId);
    Task<JobDetailForUpdateResponseDto> GetJobDetailForUpdateAsync(ClaimsPrincipal user, Guid jobId);

    Task<JobDetailResponseDto> UpdateJobAsync(ClaimsPrincipal user, Guid jobId,
        CreateUpdateJobRequestDto createUpdateJobRequestDto);

    Task<BumpJobResponseDto> BumpJobAsync(ClaimsPrincipal user, Guid jobId);
    Task<JobDetailResponseDto> CloseJobAsync(ClaimsPrincipal user, Guid jobId);
    Task<JobDetailResponseDto> ReopenJobAsync(ClaimsPrincipal user, Guid jobId);
    Task<JobApplicationDetailResponseDto> GetJobApplicationDetailAsync(ClaimsPrincipal user, Guid jobApplicationId);
    Task<JobApplicationDetailResponseDto> AcceptJobApplicationAsync(ClaimsPrincipal user, Guid jobApplicationId);
    Task<JobApplicationDetailResponseDto> RejectJobApplicationAsync(ClaimsPrincipal user, Guid jobApplicationId);
    Task<int> CloseExpiredJobsAsync(CancellationToken cancellationToken = default);
}

public class JobSummaryResponseDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required int ApplicationCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public required DateTime LastBumpedAt { get; set; }
    public required JobStatus Status { get; set; }
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
    public required DateOnly ExpiryAt { get; set; }
    public required JobStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public required DateTime LastBumpedAt { get; set; }
    public required string IndustryName { get; set; }
    public required List<JobScheduleResponseDto> JobSchedules { get; set; }
    public required List<JobSalaryResponseDto> JobSalaries { get; set; }
    public required List<JobApplicationSummaryResponseDto> JobApplications { get; set; }
}

public class PostsQuotaResponseDto
{
    public required int MaxActivePosts { get; set; }
    public required int CurrentActivePosts { get; set; }
    public int BumpBalance { get; set; }
}

public class BumpJobResponseDto
{
    public required Guid JobId { get; set; }
    public required DateTime LastBumpedAt { get; set; }
    public required int RemainingBumpBalance { get; set; }
}

public class CreateUpdateJobRequestDto
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

    [EnumDataType(typeof(Gender), ErrorMessage = "Giới tính yêu cầu không hợp lệ.")]
    public Gender? GenderRequirement { get; set; }

    [Range(15, 100, ErrorMessage = "Tuổi tối thiểu phải lớn hơn hoặc bằng 15 và nhỏ hơn hoặc bằng 100.")]
    public int? MinAgeRequirement { get; set; }

    [Range(15, 100, ErrorMessage = "Tuổi tối đa phải lớn hơn hoặc bằng 15 và nhỏ hơn hoặc bằng 100.")]
    public int? MaxAgeRequirement { get; set; }

    [Required(ErrorMessage = "Hạn nhận hồ sơ là bắt buộc.")]
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
    public Guid? IndustryId { get; set; }

    [Required(ErrorMessage = "Lịch làm việc là bắt buộc.")]
    [Length(1, BusinessRuleConstants.Jobs.MaxJobSchedulesPerJob,
        ErrorMessage = "Số lượng lịch làm việc phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng {1}.")]
    public List<JobScheduleRequestDto>? JobSchedules { get; set; }

    [Required(ErrorMessage = "Lương là bắt buộc.")]
    [Length(1, BusinessRuleConstants.Jobs.MaxJobSalariesPerJob,
        ErrorMessage = "Số lượng lương phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng {1}.")]
    public List<JobSalaryRequestDto>? JobSalaries { get; set; }
}

public class CanUpdateJobResponseDto
{
    public required bool CanUpdate { get; set; }
    public string? Reason { get; set; }
}

public class JobDetailForUpdateResponseDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int? VacancyCount { get; set; }
    public Gender? GenderRequirement { get; set; }
    public int? MinAgeRequirement { get; set; }
    public int? MaxAgeRequirement { get; set; }
    public required string DetailAddress { get; set; }
    public required string CommuneCode { get; set; }
    public required DateOnly ExpiryAt { get; set; }
    public required Guid IndustryId { get; set; }
    public required List<JobScheduleResponseDto> JobSchedules { get; set; }
    public required List<JobSalaryResponseDto> JobSalaries { get; set; }
}

public class JobScheduleRequestDto
{
    [Required(ErrorMessage = "Ngày trong tuần là bắt buộc.")]
    [EnumDataType(typeof(DayOfWeek), ErrorMessage = "Ngày trong tuần không hợp lệ.")]
    public DayOfWeek? DayOfWeek { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
    public TimeOnly? EndTime { get; set; }
}

public class JobSalaryRequestDto
{
    [Required(ErrorMessage = "Số tiền là bắt buộc.")]
    [Range(typeof(decimal), "1000", "500000000",
        ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 1,000 và nhỏ hơn hoặc bằng 500,000,000.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Số lượng thời gian là bắt buộc.")]
    [Range(1, 1000, ErrorMessage = "Số lượng thời gian phải lớn hơn hoặc bằng 1 và nhỏ hơn hoặc bằng 1000.")]
    public int? TimeQuantity { get; set; }

    [Required(ErrorMessage = "Đơn vị thời gian là bắt buộc.")]
    [EnumDataType(typeof(JobSalaryTimeUnit), ErrorMessage = "Đơn vị thời gian không hợp lệ.")]
    public JobSalaryTimeUnit? TimeUnit { get; set; }
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
    public string? CandidateFullName { get; set; }
    public required string CandidatePhoneNumber { get; set; }
    public required JobApplicationStatus Status { get; set; }
    public required DateTime AppliedAt { get; set; }
}

public class CandidateScheduleResponseDto
{
    public required Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required Shift Shift { get; set; }
}

public class JobApplicationDetailResponseDto
{
    public required Guid Id { get; set; }
    public string? CandidateAvatarUrl { get; set; }
    public string? CandidateBio { get; set; }
    public string? CandidateFullName { get; set; }
    public required string CandidatePhoneNumber { get; set; }
    public DateOnly? CandidateDateOfBirth { get; set; }
    public Gender? CandidateGender { get; set; }
    public string? CandidatePreferredWorkCommuneName { get; set; }
    public string? CandidatePreferredWorkProvinceName { get; set; }
    public required List<CandidateScheduleResponseDto> CandidatePreferredWorkSchedules { get; set; }
    public required JobApplicationStatus Status { get; set; }
    public required DateTime AppliedAt { get; set; }
}