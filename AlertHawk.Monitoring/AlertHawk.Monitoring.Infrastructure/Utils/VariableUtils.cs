namespace AlertHawk.Monitoring.Infrastructure.Utils;

public static class VariableUtils
{
    public static bool GetBoolEnvVariable(string variable)
    {
        string? enableScreenshotStorageAccount = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(enableScreenshotStorageAccount) &&
            bool.TryParse(enableScreenshotStorageAccount, out bool result))
        {
            return result;
        }

        // Default value if environment variable is not set or not a valid boolean
        return false;
    }
    
    public static int? GetIntEnvVariable(string variable)
    {
        string enableScreenshot = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(enableScreenshot) && int.TryParse(enableScreenshot, out int result))
        {
            return result;
        }

        // Default value if environment variable is not set or not a valid boolean
        return null;
    }
}