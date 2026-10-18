using Domain.Models.Jobs;

namespace Domain.Models.Users;

public class CensorProfile
{
    public Guid UserId { get; set; }

    public User? User { get; set; }
    public ICollection<EmployerRegistrationRequest>? EmployerRegistrationRequests { get; set; }
    public ICollection<JobReport>? JobReports { get; set; }
}