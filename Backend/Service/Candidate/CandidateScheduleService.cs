using System.Security.Claims;
using Domain;
using Domain.Models.Users;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Service.Candidate
{
    public class CandidateScheduleService(ApplicationDbContext dbContext)
    {
        public async Task<List<CandidateScheduleDto>> GetSchedulesAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();

            var exists = await dbContext.Candidates.AnyAsync(c => c.UserId == userId);
            if (!exists)
            {
                throw new NotFoundException("Sinh viên không tồn tại");
            }

            var schedules = await dbContext.CandidateSchedules
                .AsNoTracking()
                .Where(s => s.CandidateId == userId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.Shift)
                .ToListAsync();

            return schedules.ToDtoList();
        }
        public async Task<List<CandidateScheduleDto>> UpdateSchedulesAsync(
        ClaimsPrincipal user,
        UpdateCandidateScheduleDto request)
        {
            var userId = user.GetUserId();

            var candidate = await dbContext.Candidates
                .Include(c => c.PreferredWorkSchedules)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (candidate == null)
            {
                throw new NotFoundException("Hồ sơ ứng viên chưa được tạo.");
            }
            // Lọc trùng lặp DayOfWeek + Shift từ phía frontend gửi lên
            var distinctSlots = request.Schedules
                .GroupBy(s => new { s.DayOfWeek, s.Shift })
                .Select(g => g.First())
                .ToList();

            if (candidate.PreferredWorkSchedules != null && candidate.PreferredWorkSchedules.Count > 0)
            {
                dbContext.CandidateSchedules.RemoveRange(candidate.PreferredWorkSchedules);
            }

            var newSchedules = distinctSlots
                .Select(dto => dto.ToEntity(userId))
                .ToList();

            await dbContext.CandidateSchedules.AddRangeAsync(newSchedules);
            await dbContext.SaveChangesAsync();

            return newSchedules.ToDtoList();

        }
    }
    public class CandidateScheduleDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public Shift Shift { get; set; }
    }

    public class UpdateCandidateScheduleDto
    {
        public List<CandidateScheduleDto> Schedules { get; set; } = [];
    }
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class CandidateScheduleMapper
    {
        public static partial CandidateSchedule ToEntity(this CandidateScheduleDto dto, Guid candidateId);

        public static partial CandidateScheduleDto ToDto(this CandidateSchedule schedule);

        public static partial List<CandidateScheduleDto> ToDtoList(this IEnumerable<CandidateSchedule> schedules);
    }
}