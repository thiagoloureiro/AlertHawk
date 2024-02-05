using System.Diagnostics;
using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using Polly;
using Sentry;
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

    public async Task<MonitorHttp> CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        try
        {
            /*
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .OrResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: monitorHttp.Retries, // Number of retries
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100),
                    onRetryAsync: async (exception, retryCount) =>
                    {
                        if (exception is HttpRequestException)
                        {
                            Console.WriteLine(
                                $"Retry {retryCount} after HTTP request exception: {exception.Exception.Message}");
                        }
                        else if (exception is TimeoutException)
                        {
                            Console.WriteLine($"Retry {retryCount} after Timeout exception");
                        }
                        else if (exception is DelegateResult<HttpResponseMessage> result && result != null)
                        {
                            Console.WriteLine($"Retry {retryCount} after status code: {result.Result?.StatusCode}");
                        }
                    }
                );
            */

            using HttpClientHandler handler = new HttpClientHandler();

            // Set the maximum number of automatic redirects
            handler.MaxAutomaticRedirections = monitorHttp.MaxRedirects;

            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.36.1"); 
            client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
            client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

            var sw = new Stopwatch();
            sw.Start();
            HttpResponseMessage response = await client.GetAsync(monitorHttp.UrlToCheck);
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
                if (monitorHttp.LastStatus) // only send notification when goes from online to offline to avoid flood
                {
                    await HandleFailedNotifications(monitorHttp);
                }
            }

            await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded);
            var monitorHistory = new MonitorHistory
            {
                MonitorId = monitorHttp.MonitorId,
                Status = succeeded,
                StatusCode = (int)monitorHttp.ResponseStatusCode,
                TimeStamp = DateTime.UtcNow,
                ResponseTime = monitorHttp.ResponseTime
            };

            await _monitorRepository.SaveMonitorHistory(monitorHistory);

            return monitorHttp;
        }

        catch (Exception e)
        {
            await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, false);

            var monitorHistory = new MonitorHistory
            {
                MonitorId = monitorHttp.MonitorId,
                Status = false,
                StatusCode = (int)monitorHttp.ResponseStatusCode,
                TimeStamp = DateTime.UtcNow,
                ResponseTime = monitorHttp.ResponseTime
            };

            await _monitorRepository.SaveMonitorHistory(monitorHistory);
           
            if (monitorHttp.LastStatus) // only send notification when goes from online to offline to avoid flood
            {
                await HandleFailedNotifications(monitorHttp);
            }
        }

        return null;
    }
}