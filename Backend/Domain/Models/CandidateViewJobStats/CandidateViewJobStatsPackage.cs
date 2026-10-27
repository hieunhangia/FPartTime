namespace Domain.Models.CandidateViewJobStats;

public class CandidateViewJobStatsPackage
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required int ViewCredits { get; set; }
    public required decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
