namespace Repository.Models.Users;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public required DateTime ExpiryDate { get; set; }
    public required bool IsUsed { get; set; }
    public DateTime? UsedDate { get; set; }
    public required bool IsRevoked { get; set; }
    public required string UserId { get; set; }

    public User? User { get; set; }
}