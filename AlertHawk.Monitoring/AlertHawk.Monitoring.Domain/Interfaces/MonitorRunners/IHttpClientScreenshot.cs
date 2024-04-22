namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface IHttpClientScreenshot
{
    Task<string?> TakeScreenshotAsync(string url, int monitorId, string monitorName);
}