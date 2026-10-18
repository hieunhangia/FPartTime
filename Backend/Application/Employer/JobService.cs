using System.Security.Claims;
using Contracts.Application;
using Contracts.Application.Employer;
using Contracts.External;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Extensions;
using Repository.Models.Jobs;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class JobService(
    ApplicationDbContext context,
    INotificationsService notificationsService,
    IObjectStorageService objectStorageService) : IJobService
{
    public IQueryable<JobSummaryResponseDto> GetMyCreatedJobsQueryable(ClaimsPrincipal user) =>
        context.Jobs.AsNoTracking()
            .Where(j => j.EmployerId == user.GetUserId())
            .ProjectToSummaryDto();

    public async Task<JobDetailResponseDto> GetJobDetailAsync(ClaimsPrincipal user, Guid jobId) =>
        await context.Jobs.AsNoTracking()
            .Where(j => j.Id == jobId && j.EmployerId == user.GetUserId())
            .ProjectToDetailDto()
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Không tìm thấy công việc.");

    public async Task<PostsQuotaResponseDto> GetPostsQuotaAsync(ClaimsPrincipal user)
    {
        var employer = await context.Employers.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.GetUserId());
        if (employer == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        return new PostsQuotaResponseDto
        {
            MaxActivePosts = employer.MaxActivePosts,
            CurrentActivePosts =
                await context.Jobs.CountAsync(j => j.EmployerId == user.GetUserId() && j.Status == JobStatus.Active)
        };
    }

    public async Task<JobDetailResponseDto> CreateJobAsync(ClaimsPrincipal user,
        CreateUpdateJobRequestDto createUpdateJobRequestDto)
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

        await ValidateForCreationOrUpdateJobAsync(createUpdateJobRequestDto);
        var job = context.Jobs.Add(createUpdateJobRequestDto.MapToEntity(user.GetUserId()));
        await context.SaveChangesAsync();
        return await GetJobDetailAsync(user, job.Entity.Id);
    }

    public async Task<CanUpdateJobResponseDto> CanUpdateJobAsync(ClaimsPrincipal user, Guid jobId)
    {
        var job = await context.Jobs.AsNoTracking()
            .Where(j => j.Id == jobId && j.EmployerId == user.GetUserId())
            .FirstOrDefaultAsync();

        if (job == null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        if (job.Status == JobStatus.AdminHidden)
        {
            return new CanUpdateJobResponseDto
            {
                CanUpdate = false,
                Reason = "Không thể cập nhật công việc đã bị ẩn bởi quản trị viên."
            };
        }

        if (await context.JobApplications.AnyAsync(ja =>
                ja.JobId == jobId && ja.Status != JobApplicationStatus.Rejected))
        {
            return new CanUpdateJobResponseDto
            {
                CanUpdate = false,
                Reason = "Không thể cập nhật công việc đã có hồ sơ ứng tuyển đang chờ xử lý hoặc đã được chấp nhận."
            };
        }

        return new CanUpdateJobResponseDto { CanUpdate = true };
    }

    public async Task<JobDetailForUpdateResponseDto> GetJobDetailForUpdateAsync(ClaimsPrincipal user, Guid jobId) =>
        await context.Jobs.AsNoTracking()
            .Where(j => j.Id == jobId && j.EmployerId == user.GetUserId())
            .ProjectToDetailForUpdateDto()
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Không tìm thấy công việc.");

    public async Task<JobDetailResponseDto> UpdateJobAsync(ClaimsPrincipal user, Guid jobId,
        CreateUpdateJobRequestDto createUpdateJobRequestDto)
    {
        var job = await context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == user.GetUserId());
        if (job == null)
        {
            throw new NotFoundException("Không tìm thấy công việc cần cập nhật.");
        }

        var canUpdateJobResponse = await CanUpdateJobAsync(user, jobId);
        if (!canUpdateJobResponse.CanUpdate)
        {
            throw new BadRequestException(canUpdateJobResponse.Reason ?? "Không thể cập nhật công việc.");
        }

        await ValidateForCreationOrUpdateJobAsync(createUpdateJobRequestDto);
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.JobSchedules.Where(s => s.JobId == jobId).ExecuteDeleteAsync();
        await context.JobSalaries.Where(s => s.JobId == jobId).ExecuteDeleteAsync();
        createUpdateJobRequestDto.MapToEntity(job);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetJobDetailAsync(user, job.Id);
    }

    public async Task<JobDetailResponseDto> CloseJobAsync(ClaimsPrincipal user, Guid jobId)
    {
        var job = await context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == user.GetUserId());
        if (job == null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        if (job.Status != JobStatus.Active)
        {
            throw new BadRequestException("Chỉ có thể đóng các công việc đang hoạt động.");
        }

        job.Status = JobStatus.Closed;
        await context.SaveChangesAsync();

        var applicantsToNotify = await context.JobApplications
            .Where(ja => ja.JobId == job.Id && ja.Status == JobApplicationStatus.Pending)
            .Select(ja => ja.CandidateId)
            .ToListAsync();
        if (applicantsToNotify.Count > 0)
        {
            await notificationsService.CreateNotificationsAsync(applicantsToNotify,
                "Công việc bạn đã ứng tuyển đã được đóng.",
                $"Công việc '{job.Title}' mà bạn đã ứng tuyển trước đây đã được đóng. Vui lòng kiểm tra lại thông tin."
            );
        }

        return await GetJobDetailAsync(user, job.Id);
    }

    public async Task<JobDetailResponseDto> ReopenJobAsync(ClaimsPrincipal user, Guid jobId)
    {
        var job = await context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == user.GetUserId());
        if (job == null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        if (job.Status != JobStatus.Closed)
        {
            throw new BadRequestException("Chỉ có thể mở lại các công việc đã đóng.");
        }

        if (job.ExpiryAt < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("Không thể mở lại công việc đã hết hạn.");
        }

        var employer = await context.Employers.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.GetUserId());
        if (employer == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        if (employer.MaxActivePosts <=
            await context.Jobs.CountAsync(j => j.EmployerId == user.GetUserId() && j.Status == JobStatus.Active))
        {
            throw new BadRequestException(
                $"Bạn đã đạt giới hạn số lượng bài đăng công việc đang hoạt động ({employer.MaxActivePosts}). Vui lòng hủy một bài đăng công việc đang hoạt động trước khi mở lại bài đăng này.");
        }

        job.Status = JobStatus.Active;
        await context.SaveChangesAsync();

        var applicantsToNotify = await context.JobApplications
            .Where(ja => ja.JobId == job.Id && ja.Status == JobApplicationStatus.Pending)
            .Select(ja => ja.CandidateId)
            .ToListAsync();
        if (applicantsToNotify.Count > 0)
        {
            await notificationsService.CreateNotificationsAsync(applicantsToNotify,
                "Công việc bạn đã ứng tuyển đã được mở lại.",
                $"Công việc '{job.Title}' mà bạn đã ứng tuyển trước đây đã được mở lại. Hãy kiểm tra và cập nhật hồ sơ ứng tuyển của bạn nếu cần thiết."
            );
        }

        return await GetJobDetailAsync(user, job.Id);
    }

    public async Task<JobApplicationDetailResponseDto> GetJobApplicationDetailAsync(ClaimsPrincipal user,
        Guid jobApplicationId)
    {
        var jobApplicationDto = await context.JobApplications.AsNoTracking()
            .Where(ja => ja.Id == jobApplicationId && ja.Job!.EmployerId == user.GetUserId())
            .ProjectToDetailDto()
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Không tìm thấy hồ sơ ứng tuyển.");

        if (!string.IsNullOrEmpty(jobApplicationDto.CandidateAvatarUrl))
        {
            jobApplicationDto.CandidateAvatarUrl =
                await objectStorageService.GetPublicUrlAsync(jobApplicationDto.CandidateAvatarUrl);
        }

        return jobApplicationDto;
    }

    public async Task<JobApplicationDetailResponseDto> AcceptJobApplicationAsync(ClaimsPrincipal user,
        Guid jobApplicationId)
    {
        var jobApplication = await context.JobApplications
            .Include(ja => ja.Job)
            .FirstOrDefaultAsync(ja => ja.Id == jobApplicationId && ja.Job!.EmployerId == user.GetUserId());

        if (jobApplication == null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ứng tuyển.");
        }

        if (jobApplication.Job!.Status != JobStatus.Active)
        {
            throw new BadRequestException("Chỉ có thể chấp nhận hồ sơ ứng tuyển cho các công việc đang hoạt động.");
        }

        if (jobApplication.Status != JobApplicationStatus.Pending)
        {
            throw new BadRequestException("Chỉ có thể chấp nhận các hồ sơ ứng tuyển đang chờ xử lý.");
        }

        jobApplication.Status = JobApplicationStatus.Accepted;
        await context.SaveChangesAsync();

        await notificationsService.CreateNotificationAsync(jobApplication.CandidateId,
            "Hồ sơ ứng tuyển đã được chấp nhận.",
            $"Hồ sơ ứng tuyển của bạn cho công việc '{jobApplication.Job!.Title}' đã được chấp nhận. Hãy chuẩn bị cho bước tiếp theo trong quá trình tuyển dụng."
        );

        return await GetJobApplicationDetailAsync(user, jobApplication.Id);
    }

    public async Task<JobApplicationDetailResponseDto> RejectJobApplicationAsync(ClaimsPrincipal user,
        Guid jobApplicationId)
    {
        var jobApplication = await context.JobApplications
            .Include(ja => ja.Job)
            .FirstOrDefaultAsync(ja => ja.Id == jobApplicationId && ja.Job!.EmployerId == user.GetUserId());

        if (jobApplication == null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ứng tuyển.");
        }

        if (jobApplication.Job!.Status == JobStatus.AdminHidden)
        {
            throw new BadRequestException("Không thể từ chối hồ sơ ứng tuyển cho các công việc đã bị ẩn.");
        }

        if (jobApplication.Status != JobApplicationStatus.Pending)
        {
            throw new BadRequestException("Chỉ có thể từ chối các hồ sơ ứng tuyển đang chờ xử lý.");
        }

        jobApplication.Status = JobApplicationStatus.Rejected;
        await context.SaveChangesAsync();

        await notificationsService.CreateNotificationAsync(jobApplication.CandidateId,
            "Hồ sơ ứng tuyển đã bị từ chối.",
            $"Hồ sơ ứng tuyển của bạn cho công việc '{jobApplication.Job!.Title}' đã bị từ chối. Hãy kiểm tra lại thông tin và cải thiện hồ sơ của bạn cho các cơ hội tiếp theo."
        );

        return await GetJobApplicationDetailAsync(user, jobApplication.Id);
    }

    public async Task<int> CloseExpiredJobsAsync(CancellationToken cancellationToken = default) =>
        await context.Jobs.Where(x =>
                x.Status == JobStatus.Active && x.ExpiryAt < DateOnly.FromDateTime(DateTime.UtcNow))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, JobStatus.Closed), cancellationToken);

    private async Task ValidateForCreationOrUpdateJobAsync(CreateUpdateJobRequestDto createUpdateJobRequestDto)
    {
        if (createUpdateJobRequestDto is { MinAgeRequirement: not null, MaxAgeRequirement: not null } &&
            createUpdateJobRequestDto.MinAgeRequirement.Value > createUpdateJobRequestDto.MaxAgeRequirement.Value)
        {
            throw new BadRequestException("Tuổi tối thiểu không được lớn hơn tuổi tối đa.");
        }

        if (createUpdateJobRequestDto.GenderRequirement.HasValue &&
            !Enum.IsDefined(createUpdateJobRequestDto.GenderRequirement.Value))
        {
            throw new BadRequestException("Giới tính yêu cầu không hợp lệ.");
        }

        if (!createUpdateJobRequestDto.ExpiryAt.HasValue)
        {
            throw new BadRequestException("Hạn nhận hồ sơ là bắt buộc.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (createUpdateJobRequestDto.ExpiryAt.Value <= today)
        {
            throw new BadRequestException("Hạn nhận hồ sơ phải lớn hơn ngày hiện tại.");
        }

        var maxExpiryDate = today.AddDays(BusinessRuleConstants.Jobs.MaxJobExpiryDays);
        if (createUpdateJobRequestDto.ExpiryAt.Value > maxExpiryDate)
        {
            throw new BadRequestException(
                $"Hạn nhận hồ sơ không được vượt quá {BusinessRuleConstants.Jobs.MaxJobExpiryDays} ngày kể từ ngày tạo hoặc chỉnh sửa (tối đa đến ngày {maxExpiryDate:dd/MM/yyyy}).");
        }

        if (createUpdateJobRequestDto.JobSchedules is null || createUpdateJobRequestDto.JobSchedules.Count == 0)
        {
            throw new BadRequestException("Lịch làm việc là bắt buộc.");
        }

        foreach (var schedule in createUpdateJobRequestDto.JobSchedules)
        {
            if (schedule is null)
            {
                throw new BadRequestException("Lịch làm việc không được chứa giá trị rỗng.");
            }

            if (!schedule.DayOfWeek.HasValue)
            {
                throw new BadRequestException("Ngày trong tuần là bắt buộc.");
            }

            if (!Enum.IsDefined(schedule.DayOfWeek.Value))
            {
                throw new BadRequestException("Ngày trong tuần không hợp lệ.");
            }

            if (!schedule.StartTime.HasValue)
            {
                throw new BadRequestException("Thời gian bắt đầu là bắt buộc.");
            }

            if (!schedule.EndTime.HasValue)
            {
                throw new BadRequestException("Thời gian kết thúc là bắt buộc.");
            }

            if (schedule.StartTime.Value >= schedule.EndTime.Value)
            {
                throw new BadRequestException(
                    $"Ca làm việc ngày {GetVietnameseDayOfWeek(schedule.DayOfWeek.Value)} có thời gian kết thúc phải lớn hơn thời gian bắt đầu. Nếu ca làm việc kéo dài qua nửa đêm, vui lòng chia thành 2 ca làm việc riêng biệt.");
            }

            if ((schedule.EndTime.Value - schedule.StartTime.Value) < TimeSpan.FromMinutes(30))
            {
                throw new BadRequestException(
                    $"Ca làm việc ngày {GetVietnameseDayOfWeek(schedule.DayOfWeek.Value)} phải kéo dài tối thiểu 30 phút.");
            }
        }

        foreach (var group in createUpdateJobRequestDto.JobSchedules.GroupBy(s => s.DayOfWeek!.Value))
        {
            var orderedSchedules = group.OrderBy(s => s.StartTime!.Value).ToList();
            for (var i = 1; i < orderedSchedules.Count; i++)
            {
                var cur = orderedSchedules[i];
                var prev = orderedSchedules[i - 1];
                if (cur.StartTime!.Value < prev.EndTime!.Value)
                {
                    throw new BadRequestException(
                        $@"Các ca làm việc trong ngày {GetVietnameseDayOfWeek(cur.DayOfWeek!.Value)} bị trùng hoặc chồng chéo thời gian ({prev.StartTime!.Value:HH\:mm}-{prev.EndTime!.Value:HH\:mm} và {cur.StartTime!.Value:HH\:mm}-{cur.EndTime!.Value:HH\:mm}).");
                }
            }
        }

        if (createUpdateJobRequestDto.JobSalaries is null || createUpdateJobRequestDto.JobSalaries.Count == 0)
        {
            throw new BadRequestException("Lương là bắt buộc.");
        }

        foreach (var salary in createUpdateJobRequestDto.JobSalaries)
        {
            if (salary is null)
            {
                throw new BadRequestException("Thông tin lương không được chứa giá trị rỗng.");
            }

            if (!salary.Amount.HasValue)
            {
                throw new BadRequestException("Số tiền là bắt buộc.");
            }

            if (!salary.TimeQuantity.HasValue)
            {
                throw new BadRequestException("Số lượng thời gian là bắt buộc.");
            }

            if (!salary.TimeUnit.HasValue)
            {
                throw new BadRequestException("Đơn vị thời gian là bắt buộc.");
            }

            if (!Enum.IsDefined(salary.TimeUnit.Value))
            {
                throw new BadRequestException("Đơn vị thời gian không hợp lệ.");
            }

            if (salary.Amount.Value % 1 != 0)
            {
                throw new BadRequestException(
                    $"Số tiền lương ({salary.Amount.Value:N0}) không được chứa số lẻ thập phân.");
            }

            switch (salary.TimeUnit.Value)
            {
                case JobSalaryTimeUnit.Hour when salary.TimeQuantity.Value > 24:
                    throw new BadRequestException("Số giờ cho một định mức lương không được vượt quá 24 giờ.");
                case JobSalaryTimeUnit.Day when salary.TimeQuantity.Value > 31:
                    throw new BadRequestException("Số ngày cho một định mức lương không được vượt quá 31 ngày.");
                case JobSalaryTimeUnit.Month when salary.TimeQuantity.Value > 12:
                    throw new BadRequestException("Số tháng cho một định mức lương không được vượt quá 12 tháng.");
                case JobSalaryTimeUnit.Week when salary.TimeQuantity.Value > 52:
                    throw new BadRequestException("Số tuần cho một định mức lương không được vượt quá 52 tuần.");
                case JobSalaryTimeUnit.Session when salary.TimeQuantity.Value > 2:
                    throw new BadRequestException("Số buổi cho một định mức lương không được vượt quá 2 buổi.");
                case JobSalaryTimeUnit.Project when salary.TimeQuantity.Value > 1:
                    throw new BadRequestException("Số dự án cho một định mức lương không được vượt quá 1 dự án.");
                default:
                    throw new BadRequestException("Đơn vị thời gian không hợp lệ.");
            }
        }

        if (createUpdateJobRequestDto.JobSalaries.GroupBy(s => new { s.TimeUnit, s.TimeQuantity })
            .Any(g => g.Count() > 1))
        {
            throw new BadRequestException("Không được thiết lập trùng lặp đơn vị tính lương và thời gian.");
        }

        if (!createUpdateJobRequestDto.IndustryId.HasValue)
        {
            throw new BadRequestException("Ngành nghề là bắt buộc.");
        }

        if (createUpdateJobRequestDto.IndustryId.Value == Guid.Empty)
        {
            throw new BadRequestException("Ngành nghề không hợp lệ.");
        }

        if (!await context.Industries.AnyAsync(i => i.Id == createUpdateJobRequestDto.IndustryId.Value))
        {
            throw new BadRequestException("Ngành nghề không tồn tại.");
        }

        if (!await context.Communes.AnyAsync(c => c.Code == createUpdateJobRequestDto.CommuneCode))
        {
            throw new BadRequestException("Xã/Phường không hợp lệ hoặc không tồn tại.");
        }

        return;

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
    [MapProperty($"{nameof(Job.JobApplications)}.{nameof(Job.JobApplications.Count)}",
        nameof(JobSummaryResponseDto.ApplicationCount))]
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

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial Job MapToEntity(this CreateUpdateJobRequestDto createUpdateJobRequestDto, Guid employerId);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void MapToEntity(this CreateUpdateJobRequestDto createUpdateJobRequestDto, Job job);

    public static partial IQueryable<JobDetailForUpdateResponseDto> ProjectToDetailForUpdateDto(
        this IQueryable<Job> job);
}