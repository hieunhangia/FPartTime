using System.Security.Claims;
using Contracts.Application.Employer;
using Contracts.Common;
using Contracts.External;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Models.EmployerSubscriptions;
using Domain.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class CandidateService(
    ApplicationDbContext context,
    IObjectStorageService objectStorageService) : ICandidateService
{
    public async Task<PagedResponseDto<RecommendedCandidateResponseDto>> GetRecommendedCandidatesAsync(
        ClaimsPrincipal user, Guid jobId, PaginationRequestDto? pagination = null)
    {
        var employerId = user.GetUserId();
        var sub = await GetActiveSubscriptionAsync(employerId);
        if (!sub.CanRecommendCandidates)
        {
            throw new ForbiddenException($"Gói dịch vụ hiện tại ({sub.Tier}) không hỗ trợ tính năng gợi ý ứng viên.");
        }

        var job = await context.Jobs.AsNoTracking()
            .Include(j => j.Commune)
            .ThenInclude(c => c!.Province)
            .Include(j => j.JobSchedules)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == employerId);

        if (job == null)
        {
            throw new NotFoundException("Không tìm thấy công việc hoặc bạn không có quyền truy cập công việc này.");
        }

        if (job.Status != JobStatus.Active)
        {
            throw new BadRequestException("Chỉ có thể gợi ý ứng viên cho công việc đang ở trạng thái hoạt động.");
        }

        var appliedCandidateIds = await context.JobApplications.AsNoTracking()
            .Where(ja => ja.JobId == jobId)
            .Select(ja => ja.CandidateId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var candidates = await context.Candidates.AsNoTracking()
            .Where(c => !appliedCandidateIds.Contains(c.UserId) && c.OpenForJobUntil != null && c.OpenForJobUntil > now)
            .Include(c => c.User)
            .Include(c => c.PreferredWorkCommune)
            .ThenInclude(cm => cm!.Province)
            .Include(c => c.PreferredWorkSchedules)
            .ToListAsync();

        var today = DateOnly.FromDateTime(now);
        var recommendedList = new List<RecommendedCandidateResponseDto>();
        foreach (var candidate in candidates)
        {
            var reasons = new List<string>();
            var locationScore = 0;
            var genderScore = 0;
            var ageScore = 0;
            var scheduleScore = 0;

            if (!string.IsNullOrEmpty(candidate.PreferredWorkCommuneCode))
            {
                if (candidate.PreferredWorkCommuneCode == job.CommuneCode)
                {
                    locationScore = 35;
                    reasons.Add($"Cùng khu vực làm việc ({job.Commune?.Name ?? job.CommuneCode})");
                }
                else if (candidate.PreferredWorkCommune?.ProvinceCode != null &&
                         job.Commune?.ProvinceCode != null &&
                         candidate.PreferredWorkCommune.ProvinceCode == job.Commune.ProvinceCode)
                {
                    locationScore = 20;
                    reasons.Add($"Cùng tỉnh/thành phố ({job.Commune?.Province?.Name ?? "khu vực lân cận"})");
                }
            }

            if (job.GenderRequirement == null)
            {
                genderScore = 20;
                reasons.Add("Phù hợp tiêu chí giới tính (công việc không yêu cầu giới tính cụ thể)");
            }
            else if (candidate.Gender.HasValue && candidate.Gender == job.GenderRequirement)
            {
                genderScore = 20;
                reasons.Add($"Phù hợp giới tính yêu cầu ({GetGenderDisplayName(job.GenderRequirement.Value)})");
            }

            int? age = null;
            if (candidate.DateOfBirth.HasValue)
            {
                age = today.Year - candidate.DateOfBirth.Value.Year;
                if (candidate.DateOfBirth.Value > today.AddYears(-age.Value))
                {
                    age--;
                }
            }

            if (!job.MinAgeRequirement.HasValue && !job.MaxAgeRequirement.HasValue)
            {
                ageScore = 25;
                reasons.Add("Phù hợp độ tuổi (công việc không giới hạn độ tuổi)");
            }
            else if (age.HasValue)
            {
                var minAge = job.MinAgeRequirement ?? 0;
                var maxAge = job.MaxAgeRequirement ?? 100;

                if (age.Value >= minAge && age.Value <= maxAge)
                {
                    ageScore = 25;
                    reasons.Add($"Độ tuổi ({age} tuổi) nằm trong khoảng yêu cầu ({minAge} - {maxAge} tuổi)");
                }
                else if (age.Value >= minAge - 5 && age.Value <= maxAge + 5)
                {
                    ageScore = 10;
                    reasons.Add($"Độ tuổi ({age} tuổi) gần với khoảng yêu cầu ({minAge} - {maxAge} tuổi)");
                }
            }

            if (job.JobSchedules == null || job.JobSchedules.Count == 0)
            {
                scheduleScore = 20;
                reasons.Add("Lịch làm việc linh hoạt");
            }
            else if (candidate.PreferredWorkSchedules == null || candidate.PreferredWorkSchedules.Count == 0)
            {
                scheduleScore = 10;
                reasons.Add("Ứng viên có thời gian rảnh linh hoạt");
            }
            else
            {
                var matchingShifts = new HashSet<string>();
                foreach (var jobSched in job.JobSchedules)
                {
                    foreach (var candSched in candidate.PreferredWorkSchedules)
                    {
                        if (jobSched.DayOfWeek == candSched.DayOfWeek &&
                            DoesScheduleOverlapShift(jobSched.StartTime, jobSched.EndTime, candSched.Shift))
                        {
                            matchingShifts.Add(
                                $"{GetDayOfWeekDisplayName(candSched.DayOfWeek)} ({GetShiftDisplayName(candSched.Shift)})");
                        }
                    }
                }

                if (matchingShifts.Count > 0)
                {
                    scheduleScore = Math.Min(20, matchingShifts.Count * 10);
                    reasons.Add($"Trùng khớp ca làm việc ({string.Join(", ", matchingShifts)})");
                }
            }

            var totalScore = locationScore + genderScore + ageScore + scheduleScore;
            if (totalScore >= 30)
            {
                recommendedList.Add(new RecommendedCandidateResponseDto
                {
                    UserId = candidate.UserId,
                    FullName = candidate.FullName,
                    AvatarUrl = candidate.AvatarFilePath,
                    Bio = candidate.Bio,
                    PhoneNumber = candidate.User?.PhoneNumber,
                    DateOfBirth = candidate.DateOfBirth,
                    Age = age,
                    Gender = candidate.Gender,
                    PreferredWorkCommuneCode = candidate.PreferredWorkCommuneCode,
                    PreferredWorkCommuneName = candidate.PreferredWorkCommune?.Name,
                    PreferredWorkProvinceName = candidate.PreferredWorkCommune?.Province?.Name,
                    PreferredWorkSchedules = candidate.PreferredWorkSchedules?.Select(s =>
                        new CandidateScheduleResponseDto
                        {
                            Id = s.Id,
                            DayOfWeek = s.DayOfWeek,
                            Shift = s.Shift
                        }).ToList() ?? [],
                    MatchScore = totalScore,
                    MatchReasons = reasons
                });
            }
        }

        var pageNumber = pagination?.PageNumber ?? 1;
        var pageSize = pagination?.PageSize ?? 10;
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var totalCount = recommendedList.Count;
        var pagedItems = recommendedList
            .OrderByDescending(c => c.MatchScore)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var item in pagedItems.Where(item => !string.IsNullOrEmpty(item.AvatarUrl)))
        {
            item.AvatarUrl = await objectStorageService.GetPublicUrlAsync(item.AvatarUrl!);
        }

        return new PagedResponseDto<RecommendedCandidateResponseDto>
        {
            TotalCount = totalCount,
            Items = pagedItems
        };
    }

    public async Task<IQueryable<CandidateSummaryResponseDto>> SearchCandidatesQueryableAsync(ClaimsPrincipal user)
    {
        var employerId = user.GetUserId();
        var sub = await GetActiveSubscriptionAsync(employerId);
        if (!sub.CanQueryCandidates)
        {
            throw new ForbiddenException(
                $"Gói dịch vụ hiện tại ({sub.Tier}) không hỗ trợ tính năng tìm kiếm ứng viên tự do.");
        }

        var now = DateTime.UtcNow;
        return context.Candidates
            .Where(c => c.OpenForJobUntil != null && c.OpenForJobUntil > now)
            .ProjectToSummaryDto();
    }

    public async Task<CandidateDetailResponseDto> GetCandidateDetailAsync(ClaimsPrincipal user, Guid candidateId)
    {
        var employerId = user.GetUserId();
        var sub = await GetActiveSubscriptionAsync(employerId);
        if (sub is { CanRecommendCandidates: false, CanQueryCandidates: false })
        {
            throw new ForbiddenException("Gói dịch vụ hiện tại không hỗ trợ xem hồ sơ chi tiết ứng viên.");
        }

        var candidate = await context.Candidates.AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.PreferredWorkCommune)
            .ThenInclude(cm => cm!.Province)
            .Include(c => c.PreferredWorkSchedules)
            .FirstOrDefaultAsync(c => c.UserId == candidateId);

        if (candidate == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin ứng viên.");
        }

        var dto = candidate.MapToDetailDto();
        if (!string.IsNullOrEmpty(dto.AvatarUrl))
        {
            dto.AvatarUrl = await objectStorageService.GetPublicUrlAsync(dto.AvatarUrl);
        }

        return dto;
    }

    private async Task<EmployerSubscription> GetActiveSubscriptionAsync(Guid employerId)
    {
        var activeSub = await context.EmployerSubscriptions.AsNoTracking()
            .Where(s => s.EmployerId == employerId && s.Status == EmployerSubscriptionStatus.Active &&
                        s.EndDate >= DateTime.UtcNow)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        return activeSub ?? throw new ForbiddenException("Bạn cần đăng ký gói dịch vụ để sử dụng tính năng này.");
    }

    private static bool DoesScheduleOverlapShift(TimeOnly jobStart, TimeOnly jobEnd, Shift shift)
    {
        if (shift == Shift.Night)
        {
            return jobStart >= new TimeOnly(22, 0) || jobEnd <= new TimeOnly(23, 59) ||
                   jobStart < new TimeOnly(0, 0) || jobEnd > new TimeOnly(6, 0);
        }

        var (shiftStart, shiftEnd) = shift switch
        {
            Shift.Morning => (new TimeOnly(6, 0), new TimeOnly(12, 0)),
            Shift.Afternoon => (new TimeOnly(12, 0), new TimeOnly(18, 0)),
            Shift.Evening => (new TimeOnly(18, 0), new TimeOnly(22, 0)),
            _ => (new TimeOnly(0, 0), new TimeOnly(23, 59))
        };

        return jobStart < shiftEnd && jobEnd > shiftStart;
    }

    private static string GetGenderDisplayName(Gender gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        _ => "Khác"
    };

    private static string GetDayOfWeekDisplayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        DayOfWeek.Saturday => "Thứ 7",
        DayOfWeek.Sunday => "Chủ nhật",
        _ => day.ToString()
    };

    private static string GetShiftDisplayName(Shift shift) => shift switch
    {
        Shift.Morning => "Ca sáng",
        Shift.Afternoon => "Ca chiều",
        Shift.Evening => "Ca tối",
        Shift.Night => "Ca đêm",
        _ => shift.ToString()
    };
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployerCandidateMapper
{
    [MapProperty(
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Province)}.{nameof(CandidateProfile.PreferredWorkCommune.Province.Code)}",
        nameof(CandidateSummaryResponseDto.PreferredWorkProvinceCode))]
    [MapProperty(
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Name)}",
        nameof(CandidateSummaryResponseDto.PreferredWorkCommuneName))]
    [MapProperty(
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Province)}.{nameof(CandidateProfile.PreferredWorkCommune.Province.Name)}",
        nameof(CandidateSummaryResponseDto.PreferredWorkProvinceName))]
    [MapProperty(
        $"{nameof(CandidateProfile.User)}.{nameof(CandidateProfile.User.PhoneNumber)}",
        nameof(CandidateSummaryResponseDto.PhoneNumber))]
    private static partial CandidateSummaryResponseDto MapToSummaryDto(this CandidateProfile candidate);

    public static partial IQueryable<CandidateSummaryResponseDto> ProjectToSummaryDto(
        this IQueryable<CandidateProfile> candidates);

    [MapProperty(
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Name)}",
        nameof(CandidateDetailResponseDto.PreferredWorkCommuneName))]
    [MapProperty(
        $"{nameof(CandidateProfile.PreferredWorkCommune)}.{nameof(CandidateProfile.PreferredWorkCommune.Province)}.{nameof(CandidateProfile.PreferredWorkCommune.Province.Name)}",
        nameof(CandidateDetailResponseDto.PreferredWorkProvinceName))]
    [MapProperty(
        $"{nameof(CandidateProfile.User)}.{nameof(CandidateProfile.User.PhoneNumber)}",
        nameof(CandidateDetailResponseDto.PhoneNumber))]
    [MapProperty(
        nameof(CandidateProfile.AvatarFilePath),
        nameof(CandidateDetailResponseDto.AvatarUrl))]
    public static partial CandidateDetailResponseDto MapToDetailDto(this CandidateProfile candidate);
}