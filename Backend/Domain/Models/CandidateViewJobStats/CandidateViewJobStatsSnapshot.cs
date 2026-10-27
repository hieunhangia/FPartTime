using Domain.Models.Jobs;
using Domain.Models.Users;

namespace Domain.Models.CandidateViewJobStats;

public class CandidateViewJobStatsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int AcceptedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CandidateProfile? Candidate { get; set; }
    public Job? Job { get; set; }
}
