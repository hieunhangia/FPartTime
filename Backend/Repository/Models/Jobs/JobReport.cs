using Repository.Models.Users;

namespace Repository.Models.Jobs;

public class JobReport
{
    public Guid Id { get; set; }
    public required string Reason { get; set; }
    public JobReportStatus Status { get; set; } = JobReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid JobId { get; set; }
    public Guid ReporterId { get; set; }

    public Job? Job { get; set; }
    public CandidateProfile? Reporter { get; set; }
}

public enum JobReportStatus
{
    Pending,
    Resolved,
    Dismissed
}