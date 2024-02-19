namespace AlertHawk.Monitoring.Infrastructure.Utils;

public static class TokenUtils
{
    public static string? GetJwtToken(string token)
    {
        // Extract the actual token value (assuming it's in the format "Bearer {token}")
        string[] tokenParts = token.Split(' ');
        if (tokenParts.Length != 2 || !tokenParts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string jwtToken = tokenParts[1];
        return jwtToken;
    }
}