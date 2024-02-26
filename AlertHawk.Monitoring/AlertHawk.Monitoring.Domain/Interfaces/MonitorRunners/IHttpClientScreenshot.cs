namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface IHttpClientScreenshot
{
    Task TakeScreenshotAsync(string url, int monitorId, string monitorName);
}