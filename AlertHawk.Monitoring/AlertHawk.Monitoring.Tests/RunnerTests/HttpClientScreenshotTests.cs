using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class HttpClientScreenshotTests : IClassFixture<HttpClientRunner>
{
    private readonly IHttpClientScreenshot _httpClientScreenshot;

    public HttpClientScreenshotTests(IHttpClientScreenshot httpClientScreenshot)
    {
        _httpClientScreenshot = httpClientScreenshot;
    }

    [Fact]
    public async Task ShouldTakeScreenshot()
    {
        // Arrange
        var url = "https://www.google.com";
        var monitorId = 1;
        var monitorName = "Test";
        Environment.SetEnvironmentVariable("enable_screenshot_storage_account", "false");
        Environment.SetEnvironmentVariable("enable_screenshot", "true");
        Environment.SetEnvironmentVariable("screenshot_folder", "/tmp/screenshots/");

        // Act
        var result = await _httpClientScreenshot.TakeScreenshotAsync(url, monitorId, monitorName);

        // Assert
        Assert.NotNull(result);
    }
}