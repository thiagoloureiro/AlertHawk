using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Polly;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    public async Task StartRunner()
    {
        var monitorHttp =
            new MonitorHttp
            {
                MaxRedirects = 5,
                Name = "Monitoring API",
                HeartBeatInterval = 60,
                Retries = 0,
                UrlToCheck = "https://dev.api.alerthawk.tech/monitoring/api/version",
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10
            };


        var result = await CheckUrlsAsync(monitorHttp);
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