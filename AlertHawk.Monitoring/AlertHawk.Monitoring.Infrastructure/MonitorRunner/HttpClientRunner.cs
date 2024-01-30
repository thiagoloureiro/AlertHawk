using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using Hangfire;
using Polly;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;
    private readonly IMonitorRepository _monitorRepository;

    public HttpClientRunner(IMonitorAgentRepository monitorAgentRepository, IMonitorRepository monitorRepository)
    {
        _monitorAgentRepository = monitorAgentRepository;
        _monitorRepository = monitorRepository;
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
            if (lstMonitorByHttpType.Any())
            {
                var httpMonitorIds = lstMonitorByHttpType.Select(x => x.Id).ToList();
                var lstMonitors = await _monitorRepository.GetHttpMonitorByIds(httpMonitorIds);

                foreach (var monitorHttp in lstMonitors)
                {
                    string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                    
                    var monitor = lstMonitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId);
                    RecurringJob.AddOrUpdate<IHttpClientRunner>(jobId, x => x.CheckUrlsAsync(monitorHttp),
                        $"*/{monitor.HeartBeatInterval} * * * *");
                }
            }
        }
    }


    public async Task<MonitorHttp> CheckUrlsAsync(MonitorHttp monitorHttp)
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
                Console.WriteLine($"{monitorHttp.UrlToCheck} returned 200 OK");
                monitorHttp.ResponseStatusCode = response.StatusCode;
                return response;
            }
            else
            {
                Console.WriteLine($"{monitorHttp.UrlToCheck} returned {response.StatusCode}");
                monitorHttp.ResponseStatusCode = response.StatusCode;
                throw new HttpRequestException($"HTTP request failed with status code: {response.StatusCode}");
            }
        });

        if (policyResult.Outcome == OutcomeType.Failure)
        {
            //Console.WriteLine($"Retry policy exhausted for {monitorHttp.UrlToCheck}. Last status code: {policyResult.FinalHandledResult?.Result?.StatusCode}");
            monitorHttp.ResponseStatusCode = HttpStatusCode.Gone; // or another appropriate status code
        }
        else
        {
            // Update status code for successful responses
            monitorHttp.ResponseStatusCode = policyResult.Result?.StatusCode ?? HttpStatusCode.OK;
        }

        return monitorHttp;
    }
}