using System.Security.Claims;

namespace Frontend.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public bool IsLoggedIn => user.Identity?.IsAuthenticated ?? false;

        public string? UserId => user.GetClaimValue(ClaimTypes.NameIdentifier);

        public string? Username => user.GetClaimValue(ClaimTypes.Name);

        private string? GetClaimValue(string claimType) => user.FindFirst(claimType)?.Value;
    }
}