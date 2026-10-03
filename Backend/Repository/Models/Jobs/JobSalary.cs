namespace Repository.Models.Jobs;

public class JobSalary
{
    public Guid Id { get; set; }
    public required decimal Amount { get; set; }
    public required int TimeQuantity { get; set; }
    public required JobSalaryTimeUnit TimeUnit { get; set; }
    public Guid JobId { get; set; }

    public Job? Job { get; set; }
}

public enum JobSalaryTimeUnit
{
    Hour,
    Session,
    Day,
    Week,
    Month,
    Project
}