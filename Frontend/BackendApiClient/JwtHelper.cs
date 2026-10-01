using Microsoft.IdentityModel.JsonWebTokens;

namespace BackendApiClient;

public static class JwtHelper
{
    public static DateTimeOffset? GetExpiry(string jwtToken)
    {
        try
        {
            var token = new JsonWebToken(jwtToken);
            if (token.ValidTo == DateTime.MinValue)
            {
                return null;
            }
            return new DateTimeOffset(token.ValidTo, TimeSpan.Zero);
        }
        catch
        {
            return null;
        }
    }
}