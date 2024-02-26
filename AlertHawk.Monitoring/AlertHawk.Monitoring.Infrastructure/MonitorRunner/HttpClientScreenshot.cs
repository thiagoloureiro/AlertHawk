using System.Text.RegularExpressions;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientScreenshot : IHttpClientScreenshot
{
    public async Task TakeScreenshotAsync(string url, int monitorId)
    {
        var screenshotEnabled = GetScreenShotEnabledVariable();

        if (screenshotEnabled)
        {
            // Set the path to the directory where you want to save the screenshot
            string screenshotDirectory = $@"/screenshots/{url.Replace("https://","").Replace("http://","").Replace("/","_")}";
            
            string result = Regex.Replace(screenshotDirectory, @"[^\w\-]+", "");
            // Ensure the directory exists, create it if necessary
            Directory.CreateDirectory(result);

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
            string fileName = $"screenshot_monitorId_{monitorId}_{DateTime.UtcNow:yyyy-MM-dd_HH:mm:ss}.png";

            // Save the screenshot
            string filePath = Path.Combine(screenshotDirectory, fileName);
            screenshot.SaveAsFile(filePath);

            Console.WriteLine($"Screenshot saved: {filePath}");
        }
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