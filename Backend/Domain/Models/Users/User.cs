using Domain.Models.Notifications;
using Domain.Models.Orders;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models.Users;

public class User : IdentityUser<Guid>
{
    public ICollection<Notification>? Notifications { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public EmployerProfile? EmployerProfile { get; set; }
    public CensorProfile? CensorProfile { get; set; }
    public ICollection<PaymentTransaction>? PaymentTransactions { get; set; }
    public ICollection<OrderBase>? Orders { get; set; }
}