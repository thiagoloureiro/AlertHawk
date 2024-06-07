using System.Diagnostics;
using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly IHttpClientScreenshot _httpClientScreenshot;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private int _daysToExpireCert;
    public int _retryIntervalMilliseconds = 6000;

    public HttpClientRunner(IMonitorRepository monitorRepository, IHttpClientScreenshot httpClientScreenshot,
        INotificationProducer notificationProducer, IMonitorAlertRepository monitorAlertRepository)
    {
        _monitorRepository = monitorRepository;
        _httpClientScreenshot = httpClientScreenshot;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
        _retryIntervalMilliseconds = Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS") != null
            ? int.Parse(Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS"))
            : 6000;
    }

    public async Task CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        int maxRetries = monitorHttp.Retries + 1;
        int retryCount = 0;

        var monitor = await _monitorRepository.GetMonitorById(monitorHttp.MonitorId);
        monitorHttp.LastStatus = monitor.Status;

        while (retryCount < maxRetries)
        {
            try
            {
                var response = await MakeHttpClientCall(monitorHttp);

                var succeeded = ((int)monitorHttp.ResponseStatusCode >= 200) &&
                                ((int)monitorHttp.ResponseStatusCode <= 299);

                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorHttp.MonitorId,
                    Status = succeeded,
                    StatusCode = (int)monitorHttp.ResponseStatusCode,
                    TimeStamp = DateTime.UtcNow,
                    ResponseTime = monitorHttp.ResponseTime,
                    HttpVersion = monitorHttp.HttpVersion,
                    ResponseMessage = $"{(int)response.StatusCode} - {response.ReasonPhrase}"
                };

                if (monitorHttp.CheckCertExpiry && _daysToExpireCert <= 0)
                {
                    succeeded = false;
                    monitorHistory.ResponseMessage = "Certificate expired";
                }

                if (succeeded)
                {
                    await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded,
                        _daysToExpireCert);
                    await _monitorRepository.SaveMonitorHistory(monitorHistory);

                    if (!monitorHttp.LastStatus)
                    {
                        await _notificationProducer.HandleSuccessNotifications(monitorHttp, response.ReasonPhrase);
                        await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
                    }

                    break;
                }
                else
                {
                    // Setting Response time to zero when the call fails.
                    monitorHttp.ResponseTime = 0;
                    
                    monitorHistory.ResponseMessage = $"{(int)response.StatusCode} - {response.ReasonPhrase}";
                    retryCount++;
                    Thread.Sleep(_retryIntervalMilliseconds);

                    if (retryCount == maxRetries)
                    {
                        await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded,
                            _daysToExpireCert);
                        await _monitorRepository.SaveMonitorHistory(monitorHistory);

                        // only send notification when goes from online into offline to avoid flood
                        if (monitorHttp.LastStatus)
                        {
                            await _notificationProducer.HandleFailedNotifications(monitorHttp,
                                response.ReasonPhrase);
                            var screenshotUrl = await _httpClientScreenshot.TakeScreenshotAsync(
                                monitorHttp.UrlToCheck,
                                monitorHttp.MonitorId, monitorHttp.Name);
                            monitorHistory.ScreenShotUrl = screenshotUrl;
                            await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);

                            break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                retryCount++;
                Thread.Sleep(_retryIntervalMilliseconds);
                // If max retries reached, update status and save history
                if (retryCount == maxRetries)
                {
                    await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, false, 0);

                    var monitorHistory = new MonitorHistory
                    {
                        MonitorId = monitorHttp.MonitorId,
                        Status = false,
                        StatusCode = (int)monitorHttp.ResponseStatusCode,
                        TimeStamp = DateTime.UtcNow,
                        ResponseTime = 0,
                        ResponseMessage = err.Message
                    };

                    await _monitorRepository.SaveMonitorHistory(monitorHistory);

                    if (monitorHttp
                        .LastStatus) // only send notification when goes from online into offline to avoid flood
                    {
                        await _notificationProducer.HandleFailedNotifications(monitorHttp, err.Message);
                        await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
                        await _httpClientScreenshot.TakeScreenshotAsync(monitorHttp.UrlToCheck,
                            monitorHttp.MonitorId, monitorHttp.Name);
                    }

                    break;
                }
            }
        }
    }

    public async Task<HttpResponseMessage> MakeHttpClientCall(MonitorHttp monitorHttp)
    {
        var notAfter = DateTime.UtcNow;

        using HttpClientHandler handler = new HttpClientHandler();
        if (monitorHttp.CheckCertExpiry)
        {
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, policyErrors) =>
            {
                if (cert != null) notAfter = cert.NotAfter;
                _daysToExpireCert = (notAfter - DateTime.UtcNow).Days;
                return true;
            };
        }

        // Set the maximum number of automatic redirects
        handler.MaxAutomaticRedirections = monitorHttp.MaxRedirects;

        HttpClient? client = null;
        try
        {
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            if (monitorHttp.Headers != null)
            {
                var newHeaders = monitorHttp.Headers;
                foreach (var header in newHeaders)
                {
                    client.DefaultRequestHeaders.Add(header.Item1, header.Item2);
                }
            }

            StringContent? content = null;

            if (monitorHttp.Body != null)
            {
                content = new StringContent(monitorHttp.Body, System.Text.Encoding.UTF8, "application/json");
            }

            client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

            var sw = new Stopwatch();
            sw.Start();
            HttpResponseMessage? response = null;

            try
            {
                response = monitorHttp.MonitorHttpMethod switch
                {
                    MonitorHttpMethod.Get => await client.GetAsync(monitorHttp.UrlToCheck),
                    MonitorHttpMethod.Post => await client.PostAsync(monitorHttp.UrlToCheck, content),
                    MonitorHttpMethod.Put => await client.PutAsync(monitorHttp.UrlToCheck, content),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var elapsed = sw.ElapsedMilliseconds;
                monitorHttp.ResponseTime = (int)elapsed;
                sw.Stop();

                monitorHttp.ResponseStatusCode = response.StatusCode;
                monitorHttp.HttpVersion = response.Version.ToString();
                return response;
            }
            finally
            {
                response?.Dispose();
            }
        }
        catch (Exception e)
        {
            client?.Dispose();
        }
        finally
        {
            client?.Dispose();
        }

        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ReasonPhrase = "Internal Server Error"
        };
    }
}