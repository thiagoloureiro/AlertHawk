using System.Diagnostics;
using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;

    private readonly IPublishEndpoint _publishEndpoint;

    public HttpClientRunner(IMonitorRepository monitorRepository,
        IPublishEndpoint publishEndpoint)
    {
        _monitorRepository = monitorRepository;
        _publishEndpoint = publishEndpoint;
    }


    private async Task HandleFailedNotifications(MonitorHttp monitorHttp)
    {
        var notificationIdList = await _monitorRepository.GetMonitorNotifications(monitorHttp.MonitorId);

        Console.WriteLine(
            $"sending notification Error calling {monitorHttp.UrlToCheck}, Response StatusCode: {monitorHttp.ResponseStatusCode}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Error calling {monitorHttp.Name}, Response StatusCode: {monitorHttp.ResponseStatusCode}"
            });
        }
    }

    private async Task HandleSuccessNotifications(MonitorHttp monitorHttp)
    {
        var notificationIdList = await _monitorRepository.GetMonitorNotifications(monitorHttp.MonitorId);

        Console.WriteLine(
            $"sending success notification calling {monitorHttp.UrlToCheck}, Response StatusCode: {monitorHttp.ResponseStatusCode}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Success calling {monitorHttp.Name}, Response StatusCode: {monitorHttp.ResponseStatusCode}"
            });
        }
    }

    public async Task CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        int retryCount = 0; 
        int maxRetries = monitorHttp.Retries;

        while (retryCount < maxRetries)
        {
            try
            {
                var notAfter = DateTime.UtcNow;
                int daysToExpireCert = 0;

                using HttpClientHandler handler = new HttpClientHandler();
                if (monitorHttp.CheckCertExpiry)
                {
                    handler.ServerCertificateCustomValidationCallback = (request, cert, chain, policyErrors) =>
                    {
                        if (cert != null) notAfter = cert.NotAfter;
                        daysToExpireCert = (notAfter - DateTime.UtcNow).Days;
                        return true;
                    };
                }

                // Set the maximum number of automatic redirects
                handler.MaxAutomaticRedirections = monitorHttp.MaxRedirects;

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.36.1");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
                client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

                var sw = new Stopwatch();
                sw.Start();
                var response = await client.GetAsync(monitorHttp.UrlToCheck);
                var elapsed = sw.ElapsedMilliseconds;
                monitorHttp.ResponseTime = (int)elapsed;
                sw.Stop();

                monitorHttp.ResponseStatusCode = response.StatusCode;

                var succeeded = ((int)monitorHttp.ResponseStatusCode >= 200) &&
                                ((int)monitorHttp.ResponseStatusCode <= 299);

                if (succeeded)
                {
                    if (monitorHttp.LastStatus == false)
                    {
                        await HandleSuccessNotifications(monitorHttp);
                    }
                }
                else
                {
                    if (monitorHttp
                        .LastStatus) // only send notification when goes from online to offline to avoid flood
                    {
                        await HandleFailedNotifications(monitorHttp);
                    }
                }

                await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded, daysToExpireCert);
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorHttp.MonitorId,
                    Status = succeeded,
                    StatusCode = (int)monitorHttp.ResponseStatusCode,
                    TimeStamp = DateTime.UtcNow,
                    ResponseTime = monitorHttp.ResponseTime
                };

                await _monitorRepository.SaveMonitorHistory(monitorHistory);

                break;
            }
            catch (TaskCanceledException err)
            {
                retryCount++;
                Thread.Sleep(5000);
                if (retryCount >= maxRetries)
                {
                    var monitorHistory = new MonitorHistory
                    {
                        MonitorId = monitorHttp.MonitorId,
                        Status = false,
                        StatusCode = (int)HttpStatusCode.RequestTimeout,
                        TimeStamp = DateTime.UtcNow,
                        ResponseTime = monitorHttp.ResponseTime
                    };

                    await SaveFailedStatus(monitorHttp, monitorHistory);
                }
            }
            catch (Exception err)
            {
                retryCount++;
                Thread.Sleep(5000);
                if (retryCount >= maxRetries)
                {
                    var monitorHistory = new MonitorHistory
                    {
                        MonitorId = monitorHttp.MonitorId,
                        Status = false,
                        StatusCode = (int)monitorHttp.ResponseStatusCode,
                        TimeStamp = DateTime.UtcNow,
                        ResponseTime = monitorHttp.ResponseTime
                    };

                    await SaveFailedStatus(monitorHttp, monitorHistory);
                }
            }
        }
    }

    private async Task SaveFailedStatus(MonitorHttp monitorHttp, MonitorHistory monitorHistory)
    {
        await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, false, 0);

        await _monitorRepository.SaveMonitorHistory(monitorHistory);

        if (monitorHttp.LastStatus) // only send notification when goes from online to offline to avoid flood
        {
            await HandleFailedNotifications(monitorHttp);
        }
    }
}