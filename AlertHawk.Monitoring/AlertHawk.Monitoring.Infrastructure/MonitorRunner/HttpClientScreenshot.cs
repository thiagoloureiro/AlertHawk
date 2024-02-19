using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientScreenshot : IHttpClientScreenshot
{
    public async Task TakeScreenshotAsync(string url, int monitorId)
    {
        // Set the path to the directory where you want to save the screenshot
        string screenshotDirectory = @"/screenshots";
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
        Thread.Sleep(2000);

        // Take screenshot
        Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();

        // Generate file name
        string fileName = $"screenshot_monitorId_{monitorId}_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        // Save the screenshot
        string filePath = Path.Combine(screenshotDirectory, fileName);
        screenshot.SaveAsFile(filePath);

        Console.WriteLine($"Screenshot saved: {filePath}");
    }
}