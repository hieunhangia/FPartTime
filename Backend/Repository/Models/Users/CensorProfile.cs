using Repository.Models.Jobs;

namespace Repository.Models.Users;

public class CensorProfile
{
    public Guid UserId { get; set; }

    public User? User { get; set; }
    public ICollection<EmployerRegistrationRequest>? EmployerRegistrationRequests { get; set; }
    public ICollection<JobReport>? JobReports { get; set; }
}