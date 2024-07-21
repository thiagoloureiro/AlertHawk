using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Infrastructure.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientScreenshot : IHttpClientScreenshot
{
    public async Task<string?> TakeScreenshotAsync(string url, int monitorId, string monitorName)
    {
        var screenshotEnabled = VariableUtils.GetBoolEnvVariable("enable_screenshot");
        string? screenshotUrl = string.Empty;
        if (screenshotEnabled)
        {
            var screenshotPath = Environment.GetEnvironmentVariable("screenshot_folder") ?? "/screenshots/";
            // Set the path to the directory where you want to save the screenshot
            string screenshotDirectory = $@"{screenshotPath}{monitorId}_{monitorName}";
            // Ensure the directory exists, create it if necessary
            Directory.CreateDirectory(screenshotDirectory);

            // Set Chrome options
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--no-sandbox");
            options.AddArgument("--headless");
            options.AddArgument("--window-size=1280,780");

            // Initialize ChromeDriver
            using var driver = new ChromeDriver(options);
            try
            {
                await driver.Navigate().GoToUrlAsync(url);

                // Wait for the page to load (adjust the wait time as needed)
                await Task.Delay(VariableUtils.GetIntEnvVariable("screenshot_wait_time_ms") ?? 3000);
                
                // Take screenshot
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();

                // Generate file name
                string fileName = $"screenshot_monitorId_{monitorId}_{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.png";

                // Save the screenshot
                string filePath = Path.Combine(screenshotDirectory, fileName);
                screenshot.SaveAsFile(filePath);
                var screenshotAsByteArray = screenshot.AsByteArray;

                if (VariableUtils.GetBoolEnvVariable("enable_screenshot_storage_account"))
                {
                    screenshotUrl =
                        await BlobUtils.UploadByteArrayToBlob($"{fileName}.jpg", screenshotAsByteArray);
                }

                return screenshotUrl;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                driver.Quit();
                return null;
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }
        }

        return null;
    }
}