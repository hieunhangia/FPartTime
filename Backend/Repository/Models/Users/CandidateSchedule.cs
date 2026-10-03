namespace Repository.Models.Users;

public class CandidateSchedule
{
    public Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; } 
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
    public Guid CandidateId { get; set; }

    public CandidateProfile? Candidate { get; set; }
}