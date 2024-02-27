using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Infrastructure.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientScreenshot : IHttpClientScreenshot
{
    public async Task<string> TakeScreenshotAsync(string url, int monitorId, string monitorName)
    {
        var screenshotEnabled = GetScreenShotEnabledVariable();
        string screenshotUrl = string.Empty;
        if (screenshotEnabled)
        {
            // Set the path to the directory where you want to save the screenshot
            string screenshotDirectory = $@"/screenshots/{monitorId}_{monitorName}";
            // Ensure the directory exists, create it if necessary
            Directory.CreateDirectory(screenshotDirectory);

            // Set Chrome options
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--no-sandbox");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddArgument("--headless");
            options.AddArgument("--window-size=1280,780");

            // Initialize ChromeDriver
            using var driver = new ChromeDriver(options);

            driver.Navigate().GoToUrl(url);

            // Wait for the page to load (adjust the wait time as needed)
            Thread.Sleep(GetScreenShotWaitTime());

            // Take screenshot
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();

            // Generate file name
            string fileName = $"screenshot_monitorId_{monitorId}_{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.png";

            // Save the screenshot
            string filePath = Path.Combine(screenshotDirectory, fileName);
            screenshot.SaveAsFile(filePath);
            var screenshotAsByteArray = screenshot.AsByteArray;

            if (GetStorageAccountEnabledVariable())
            {
                screenshotUrl = await BlobUtils.UploadByteArrayToBlob($"{monitorId}-{monitorName}", screenshotAsByteArray);
            }

            Console.WriteLine($"Screenshot saved: {filePath}");
        }

        return screenshotUrl;
    }

    static bool GetStorageAccountEnabledVariable()
    {
        string enableScreenshotStorageAccount = Environment.GetEnvironmentVariable("enable_screenshot_storage_account");
        if (!string.IsNullOrEmpty(enableScreenshotStorageAccount) &&
            bool.TryParse(enableScreenshotStorageAccount, out bool result))
        {
            return result;
        }

        // Default value if environment variable is not set or not a valid boolean
        return false;
    }

    static bool GetScreenShotEnabledVariable()
    {
        string enableScreenshot = Environment.GetEnvironmentVariable("enable_screenshot");
        if (!string.IsNullOrEmpty(enableScreenshot) && bool.TryParse(enableScreenshot, out bool result))
        {
            return result;
        }

        // Default value if environment variable is not set or not a valid boolean
        return false;
    }

    static int GetScreenShotWaitTime()
    {
        string enableScreenshot = Environment.GetEnvironmentVariable("screenshot_wait_time_ms");
        if (!string.IsNullOrEmpty(enableScreenshot) && int.TryParse(enableScreenshot, out int result))
        {
            return result;
        }

        // Default value if environment variable is not set or not a valid boolean
        return 3000;
    }
}