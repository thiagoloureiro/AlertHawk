namespace AlertHawk.Notification.Infrastructure.Utils;

public static class TokenUtils
{
    public static string? GetJwtToken(string? token)
    {
        if (token == null) return null;
        string[] tokenParts = token.Split(' ');
        if (tokenParts.Length != 2 || !tokenParts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string jwtToken = tokenParts[1];
        return jwtToken;
    }
}