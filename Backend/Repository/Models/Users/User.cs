using Microsoft.AspNetCore.Identity;

namespace Repository.Models.Users;

public class User : IdentityUser
{
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
}