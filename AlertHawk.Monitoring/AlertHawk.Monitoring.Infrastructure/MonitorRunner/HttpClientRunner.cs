using System.Diagnostics;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly IHttpClientScreenshot _httpClientScreenshot;
    private readonly INotificationProducer _notificationProducer;
    private int _daysToExpireCert = 0;

    public HttpClientRunner(IMonitorRepository monitorRepository, IHttpClientScreenshot httpClientScreenshot,
        INotificationProducer notificationProducer)
    {
        _monitorRepository = monitorRepository;
        _httpClientScreenshot = httpClientScreenshot;
        _notificationProducer = notificationProducer;
    }

    public async Task CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        int maxRetries = monitorHttp.Retries;
        int retryCount = 0;

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
                        await _monitorRepository.SaveMonitorAlert(monitorHistory);
                    }

                    break;
                }
                else
                {
                    monitorHistory.ResponseMessage = $"{(int)response.StatusCode} - {response.ReasonPhrase}";
                    retryCount++;
                    Thread.Sleep(2000);

                    if (retryCount == maxRetries)
                    {
                        await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded,
                            _daysToExpireCert);
                        await _monitorRepository.SaveMonitorHistory(monitorHistory);

                        // only send notification when goes from online to offline to avoid flood
                        if (monitorHttp.LastStatus)
                        {
                            await _notificationProducer.HandleFailedNotifications(monitorHttp,
                                response.ReasonPhrase);
                            var screenshotUrl = await _httpClientScreenshot.TakeScreenshotAsync(
                                monitorHttp.UrlToCheck,
                                monitorHttp.MonitorId, monitorHttp.Name);
                            monitorHistory.ScreenShotUrl = screenshotUrl;
                            await _monitorRepository.SaveMonitorAlert(monitorHistory);

                            break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                retryCount++;
                Thread.Sleep(2000);
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
                        ResponseTime = monitorHttp.ResponseTime,
                        ResponseMessage = err.Message
                    };

                    await _monitorRepository.SaveMonitorHistory(monitorHistory);

                    if (monitorHttp
                        .LastStatus) // only send notification when goes from online to offline to avoid flood
                    {
                        await _notificationProducer.HandleFailedNotifications(monitorHttp, err.Message);
                        await _monitorRepository.SaveMonitorAlert(monitorHistory);
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
                if (cert != null)
                {
                    notAfter = cert.NotAfter;
                }

                _daysToExpireCert = (notAfter - DateTime.UtcNow).Days;
                return true;
            };
        }

        // Set the maximum number of automatic redirects
        handler.MaxAutomaticRedirections = monitorHttp.MaxRedirects;

        using HttpClient client = new HttpClient(handler);
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

        HttpResponseMessage response = monitorHttp.MonitorHttpMethod switch
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
}