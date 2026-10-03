using Repository.Models.Users;

namespace Repository.Models.Notifications;

public class Notification
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }

    public User? User { get; set; }
}