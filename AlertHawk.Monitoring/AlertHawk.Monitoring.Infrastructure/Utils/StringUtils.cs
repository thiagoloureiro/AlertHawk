using System.Text;

namespace AlertHawk.Monitoring.Infrastructure.Utils;

public static class StringUtils
{
    public static string RandomStringGenerator()
    {
        int length = 10; // Length of the random string
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // Characters to choose from
        Random random = new Random();
        StringBuilder stringBuilder = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(chars.Length);
            stringBuilder.Append(chars[index]);
        }

        string randomString = stringBuilder.ToString();
        return randomString;
    }
}