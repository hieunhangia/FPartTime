using Repository.Models.Users;

namespace Repository.Models.Jobs;

public class JobApplication
{
    public Guid Id { get; set; }
    public JobApplicationStatus Status { get; set; } = JobApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }

    public Job? Job { get; set; }
    public CandidateProfile? Candidate { get; set; }
}

public enum JobApplicationStatus
{
    Pending,
    Accepted,
    Rejected
}