using Domain.Shared.Enums;

namespace Repository.Models.Users;

public class CandidateSchedule
{
    public Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required Shift Shift { get; set; }
    public Guid CandidateId { get; set; }

    public CandidateProfile? Candidate { get; set; }
}