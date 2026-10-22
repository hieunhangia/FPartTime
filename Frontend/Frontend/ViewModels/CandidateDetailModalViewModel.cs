using ApiSdk.Models;
using Microsoft.Kiota.Abstractions;

namespace Frontend.ViewModels;

public class CandidateDetailModalViewModel
{
    public required Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public Gender? Gender { get; set; }
    public Date? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Location { get; set; }
    public string? Bio { get; set; }
    public List<CandidateScheduleResponseDto> PreferredWorkSchedules { get; set; } = [];
    public int? MatchScore { get; set; }
    public List<string> MatchReasons { get; set; } = [];

    public string ScoreBadgeClass => MatchScore switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-primary",
        _ => "bg-secondary"
    };

    public string GenderDisplay => Gender switch
    {
        ApiSdk.Models.Gender.Male => "Nam",
        ApiSdk.Models.Gender.Female => "Nữ",
        _ => "Khác"
    };

    public string DateOfBirthDisplay =>
        DateOfBirth.HasValue
            ? $"{DateOfBirth.Value.Day:D2}/{DateOfBirth.Value.Month:D2}/{DateOfBirth.Value.Year}"
            : "-";

    public static string GetDayOfWeekDisplay(DayOfWeekObject? day) => day switch
    {
        DayOfWeekObject.Monday => "Thứ Hai",
        DayOfWeekObject.Tuesday => "Thứ Ba",
        DayOfWeekObject.Wednesday => "Thứ Tư",
        DayOfWeekObject.Thursday => "Thứ Năm",
        DayOfWeekObject.Friday => "Thứ Sáu",
        DayOfWeekObject.Saturday => "Thứ Bảy",
        DayOfWeekObject.Sunday => "Chủ Nhật",
        _ => day?.ToString() ?? "-"
    };

    public static string GetShiftDisplay(Shift? shift) => shift switch
    {
        Shift.Morning => "Ca sáng",
        Shift.Afternoon => "Ca chiều",
        Shift.Evening => "Ca tối",
        Shift.Night => "Ca đêm",
        _ => "-"
    };

    public static CandidateDetailModalViewModel FromDetailDto(CandidateDetailResponseDto dto)
    {
        int? age = null;
        if (dto.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = new DateOnly(dto.DateOfBirth.Value.Year, dto.DateOfBirth.Value.Month, dto.DateOfBirth.Value.Day);
            age = today.Year - dob.Year;
            if (dob > today.AddYears(-age.Value))
            {
                age--;
            }
        }

        var location = string.Join(", ", new[] { dto.PreferredWorkCommuneName, dto.PreferredWorkProvinceName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CandidateDetailModalViewModel
        {
            UserId = dto.UserId ?? Guid.Empty,
            FullName = dto.FullName,
            AvatarUrl = dto.AvatarUrl,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Age = age,
            Location = string.IsNullOrWhiteSpace(location) ? null : location,
            Bio = dto.Bio,
            PreferredWorkSchedules = dto.PreferredWorkSchedules ?? []
        };
    }

    public static CandidateDetailModalViewModel FromRecommendedDto(RecommendedCandidateResponseDto dto)
    {
        var location = string.Join(", ", new[] { dto.PreferredWorkCommuneName, dto.PreferredWorkProvinceName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CandidateDetailModalViewModel
        {
            UserId = dto.UserId ?? Guid.Empty,
            FullName = dto.FullName,
            AvatarUrl = dto.AvatarUrl,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Age = dto.Age,
            Location = string.IsNullOrWhiteSpace(location) ? null : location,
            Bio = dto.Bio,
            PreferredWorkSchedules = dto.PreferredWorkSchedules ?? [],
            MatchScore = dto.MatchScore,
            MatchReasons = dto.MatchReasons ?? []
        };
    }
}
