using Domain.Models.CandidateViewJobStats;

namespace Domain.Models.Orders;

public class CandidateViewJobStatsOrder : OrderBase
{
    public Guid? PackageId { get; set; }
    public required string PackageName { get; set; }
    public required int ViewCredits { get; set; }

    public CandidateViewJobStatsPackage? Package { get; set; }
}
