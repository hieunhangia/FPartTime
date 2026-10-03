namespace Repository.Models.Jobs;

public class JobSchedule
{
    public Guid Id { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
    public Guid JobId { get; set; }

    public Job? Job { get; set; }
}