using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Contracts.Application.Employer;
using Domain;
using Domain.Models.Jobs;
using Domain.Models.Users;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;


namespace Service.Candidate
{
    public class JobApplicationService(ApplicationDbContext dbContext)
    {
        public async Task<JobApplicationResponseDto> ApplyJobAsync(ClaimsPrincipal user, Guid jobId)
        {
            var userId = user.GetUserId();

            var job = await dbContext.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                throw new NotFoundException("Không có tin tuyển dụng");
            }

            if (job.Status != JobStatus.Active)
            {
                throw new BadRequestException("Tin tuyển dụng này không còn hoạt động");
            }

            if (job.EmployerId == userId)
            {
                throw new BadRequestException("Bạn không thể ứng tuyển vào công việc của chính mình");
            }

            var candidateExists = await dbContext.Candidates.AnyAsync(c => c.UserId == userId);
            if (!candidateExists)
            {
                dbContext.Candidates.Add(new CandidateProfile { UserId = userId });
                await dbContext.SaveChangesAsync();
            }

            var candidateId = userId;

            var alreadyApplied = await dbContext.JobApplications
                .AnyAsync(a => a.JobId == jobId && a.CandidateId == candidateId);

            if (alreadyApplied)
            {
                throw new ConflictException("Bạn đã ứng tuyển vào tin tuyển dụng này rồi.");
            }

            var newJobSchedules = await dbContext.JobSchedules
            .AsNoTracking()
            .Where(s => s.JobId == jobId)
            .ToListAsync();

            if (newJobSchedules.Count > 0)
            {
                var acceptedJobSchedules = await dbContext.JobApplications
                    .AsNoTracking()
                    .Where(a => a.CandidateId == candidateId
                             && a.Status == JobApplicationStatus.Accepted
                             && a.Job!.Status == JobStatus.Active)
                    .SelectMany(a => a.Job!.JobSchedules!)
                    .Select(s => new
                    {
                        s.Job!.Title,
                        s.DayOfWeek,
                        s.StartTime,
                        s.EndTime
                    })
                    .ToListAsync();
                foreach (var newSchedule in newJobSchedules)
                {
                    var conflictingSchedule = acceptedJobSchedules.FirstOrDefault(accepted =>
                        accepted.DayOfWeek == newSchedule.DayOfWeek &&
                        newSchedule.StartTime < accepted.EndTime &&
                        accepted.StartTime < newSchedule.EndTime);
                    if (conflictingSchedule != null)
                    {
                        var dayName = FormatDayOfWeek(newSchedule.DayOfWeek);
                        throw new ConflictException(
                            $"Lịch làm việc ({dayName}: {newSchedule.StartTime:HH\\:mm} - {newSchedule.EndTime:HH\\:mm}) " +
                            $"bị trùng với công việc '{conflictingSchedule.Title}' mà bạn đang làm " +
                            $"({conflictingSchedule.StartTime:HH\\:mm} - {conflictingSchedule.EndTime:HH\\:mm})."
                        );
                    }
                }
            }
            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                CandidateId = candidateId,
                Status = JobApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };
            dbContext.JobApplications.Add(application);
            await dbContext.SaveChangesAsync();
            return new JobApplicationResponseDto
            {
                Id = application.Id,
                JobId = jobId,
                JobTitle = job.Title,
                Status = application.Status,
                AppliedAt = application.AppliedAt
            };

        }


        public async Task WithdrawAsync(ClaimsPrincipal user, Guid applicationId)
        {
            var userId = user.GetUserId();

            var application = await dbContext.JobApplications
          .FirstOrDefaultAsync(a => a.Id == applicationId && a.CandidateId == userId);

            if (application == null)
            {
                throw new NotFoundException("Không tìm thấy đơn ứng tuyển");
            }

            if (application.Status != JobApplicationStatus.Pending)
            {
                throw new BadRequestException("Chỉ có thể hủy đơn ứng tuyển khi trạng thái là đang chờ xử lý");
            }

            dbContext.JobApplications.Remove(application);
            await dbContext.SaveChangesAsync();

        }

        public IQueryable<JobApplicationResponseDto> GetAllApplication(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();

            var response = dbContext.JobApplications
                .AsNoTracking()
                .Where(a => a.CandidateId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ProjectToApplicationDto();

            return response;
        }
        private static string FormatDayOfWeek(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => day.ToString()
        };

    }




    public class JobApplicationResponseDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string? JobTitle { get; set; }
        public JobApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }

    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public static partial class JobApplicationMapper
    {
        [MapProperty($"{nameof(JobApplication.Job)}.{nameof(JobApplication.Job.Title)}", nameof(JobApplicationResponseDto.JobTitle))]
        public static partial JobApplicationResponseDto MapToDto(this JobApplication application);
        public static partial IQueryable<JobApplicationResponseDto> ProjectToApplicationDto(this IQueryable<JobApplication> applications);
    }
}
