namespace Repository.Models.Users;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpiryAt { get; set; }
    public required bool IsUsed { get; set; }
    public required bool IsRevoked { get; set; }
    public Guid UserId { get; set; }

    public User? User { get; set; }
}