using Microsoft.AspNetCore.Identity;
using Repository.Models.Notifications;

namespace Repository.Models.Users;

public class User : IdentityUser<Guid>
{
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
    public ICollection<Notification>? Notifications { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public EmployerProfile? EmployerProfile { get; set; }
    public CensorProfile? CensorProfile { get; set; }
}