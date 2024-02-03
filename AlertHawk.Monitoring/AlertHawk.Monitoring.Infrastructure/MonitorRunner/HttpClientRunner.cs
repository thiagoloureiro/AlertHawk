using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using Hangfire;
using Hangfire.Storage;
using MassTransit;
using Polly;
using Sentry;
using SharedModels;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;
    private readonly IMonitorRepository _monitorRepository;

    private readonly IPublishEndpoint _publishEndpoint;

    public HttpClientRunner(IMonitorAgentRepository monitorAgentRepository, IMonitorRepository monitorRepository,
        IPublishEndpoint publishEndpoint)
    {
        _monitorAgentRepository = monitorAgentRepository;
        _monitorRepository = monitorRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task StartRunnerManager()
    {
        var tasksToMonitor = await _monitorAgentRepository.GetAllMonitorAgentTasksByAgentId(GlobalVariables.NodeId);
        if (tasksToMonitor.Any())
        {
            var monitorIds = tasksToMonitor.Select(x => x.MonitorId).ToList();
            var monitorListByIds = await _monitorRepository.GetMonitorListByIds(monitorIds);

            // HTTP
            var lstMonitorByHttpType = monitorListByIds.Where(x => x.MonitorTypeId == 1);
            var monitorByHttpType = lstMonitorByHttpType.ToList();
            if (monitorByHttpType.Any())
            {
                var httpMonitorIds = monitorByHttpType.Select(x => x.Id).ToList();
                var lstMonitors = await _monitorRepository.GetHttpMonitorByIds(httpMonitorIds);

                GlobalVariables.TaskList = httpMonitorIds;

                var lstStringsToAdd = new List<string>();
                var monitorHttps = lstMonitors.ToList();

                foreach (var monitorHttp in monitorHttps)
                {
                    monitorHttp.LastStatus =
                        monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Status;
                    monitorHttp.Name = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Name;

                    string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                    lstStringsToAdd.Add(jobId);
                }

                IEnumerable<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
                recurringJobs = recurringJobs.Where(x => x.Id.StartsWith("StartRunnerManager_CheckUrlsAsync_JobId"))
                    .ToList();

                foreach (var job in recurringJobs)
                {
                    if (!lstStringsToAdd.Contains(job.Id))
                    {
                        RecurringJob.RemoveIfExists(job.Id);
                    }
                }

                foreach (var monitorHttp in monitorHttps)
                {
                    string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                    Thread.Sleep(50);
                    var monitor = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId);
                    RecurringJob.AddOrUpdate<IHttpClientRunner>(jobId, x => x.CheckUrlsAsync(monitorHttp),
                        $"*/{monitor?.HeartBeatInterval} * * * *");
                }
            }
        }
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

            using HttpClientHandler handler = new HttpClientHandler();

            // Set the maximum number of automatic redirects
            handler.MaxAutomaticRedirections = monitorHttp.MaxRedirects;

            using HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

            var policyResult = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                HttpResponseMessage response = await client.GetAsync(monitorHttp.UrlToCheck);

                // Check if the status code is 200 OK
                if (response.IsSuccessStatusCode)
                {
                    //Console.WriteLine($"{monitorHttp.UrlToCheck} returned 200 OK");
                    monitorHttp.ResponseStatusCode = response.StatusCode;
                    return response;
                }
                else
                {
                    // Console.WriteLine($"{monitorHttp.UrlToCheck} returned {response.StatusCode}");
                    monitorHttp.ResponseStatusCode = response.StatusCode;
                    return response;
                    // throw new HttpRequestException($"HTTP request failed with status code: {response.StatusCode}");
                }
            });

            if (policyResult.Outcome == OutcomeType.Failure)
            {
                monitorHttp.ResponseStatusCode =
                    policyResult.FinalHandledResult.StatusCode; // or another appropriate status code
            }
            else
            {
                // Update status code for successful responses
                monitorHttp.ResponseStatusCode = policyResult.Result?.StatusCode ?? HttpStatusCode.OK;
            }

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
                TimeStamp = DateTime.UtcNow
            };

            await _monitorRepository.SaveMonitorHistory(monitorHistory);

            return monitorHttp;
        }

        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }

        return null;
    }
}