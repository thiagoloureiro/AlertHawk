using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private int _daysToExpireCert;
    private readonly int _retryIntervalMilliseconds = 6000;
    private readonly ILogger<HttpClientRunner> _logger;

    public HttpClientRunner(IMonitorRepository monitorRepository,
        INotificationProducer notificationProducer, IMonitorAlertRepository monitorAlertRepository,
        IMonitorHistoryRepository monitorHistoryRepository)
    {
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
        _monitorHistoryRepository = monitorHistoryRepository;
        _retryIntervalMilliseconds = Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS") != null
            ? int.Parse(Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS"))
            : 6000;
        _logger = new LoggerFactory().CreateLogger<HttpClientRunner>();
    }

    public async Task CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        int maxRetries = monitorHttp.Retries + 1;
        int retryCount = 0;

        var monitor = await _monitorRepository.GetMonitorById(monitorHttp.MonitorId);

        monitorHttp.MonitorEnvironment = monitor.MonitorEnvironment;
        monitorHttp.MonitorRegion = monitor.MonitorRegion;
        monitorHttp.LastStatus = monitor.Status;

        while (retryCount < maxRetries)
        {
            var response = await MakeHttpClientCall(monitorHttp);
            monitorHttp.ResponseStatusCode = response.StatusCode;
            try
            {
                // Fetch succeeded status based on monitor.HttpResponseCodeFrom and HttpResponseCodeTo
                var fromStatus = monitorHttp.HttpResponseCodeFrom ?? 200;
                var toStatus = monitorHttp.HttpResponseCodeTo ?? 299;

                var succeeded = ((int)monitorHttp.ResponseStatusCode >= fromStatus) &&
                                ((int)monitorHttp.ResponseStatusCode <= toStatus);

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
                    await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded, _daysToExpireCert);
                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                    if (monitorHttp.CheckMonitorHttpHeaders == true)
                    {
                        try
                        {
                            var headers = CheckHttpHeaders(response);
                            headers.MonitorId = monitorHttp.MonitorId;
                            await _monitorHistoryRepository.SaveMonitorSecurityHeaders(headers);
                        }
                        catch (Exception e)
                        {
                            SentrySdk.CaptureException(e);
                            _logger.LogError("Error checking HTTP headers: {message}", e.Message);
                        }
                    }

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

                    if(monitorHistory.ResponseMessage != "Certificate expired")
                    {
                        monitorHistory.ResponseMessage = $"{(int)response.StatusCode} - {response.ReasonPhrase}";
                    }
                    
                    retryCount++;
                    Thread.Sleep(_retryIntervalMilliseconds);

                    if (retryCount == maxRetries)
                    {
                        await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, succeeded,
                            _daysToExpireCert);
                        await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                        // only send notification when goes from online into offline to avoid flood
                        if (monitorHttp.LastStatus)
                        {
                            await _notificationProducer.HandleFailedNotifications(monitorHttp,
                                response.ReasonPhrase);

                            await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);

                            break;
                        }
                    }
                }
            }
            // catch database issue
            catch (SqlException ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError("Database connectivity issue: {message}", ex.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                retryCount++;

                // Avoid logging when it's a database connectivity issue
                if (err.Message.Contains(
                        "A network-related or instance-specific error occurred while establishing a connection to SQL Server."))
                {
                    _logger.LogError("Database connectivity issue: {message}", err.Message);
                    break; // Exit the loop on database connectivity issues
                }

                // avoid Execution Timeout Expired.
                if (err.Message.Contains("Execution Timeout Expired"))
                {
                    _logger.LogError("Execution Timeout Expired: {message}", err.Message);
                    break; // Exit the loop on execution timeout
                }

                // If max retries reached, update status and save history
                if (retryCount == maxRetries)
                {
                    await _monitorRepository.UpdateMonitorStatus(monitorHttp.MonitorId, false, 0);

                    var monitorHistory = new MonitorHistory
                    {
                        MonitorId = monitorHttp.MonitorId,
                        Status = false,
                        StatusCode = (int)response.StatusCode,
                        TimeStamp = DateTime.UtcNow,
                        ResponseTime = 0,
                        ResponseMessage = err.Message
                    };

                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                    if (monitorHttp
                        .LastStatus) // only send notification when goes from online into offline to avoid flood
                    {
                        monitorHttp.ResponseStatusCode = response.StatusCode;
                        await _notificationProducer.HandleFailedNotifications(monitorHttp, err.Message);

                        // Save monitor alert
                        await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
                    }

                    break;
                }

                Thread.Sleep(_retryIntervalMilliseconds);
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

        if (monitorHttp.IgnoreTlsSsl)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
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

            if (!string.IsNullOrEmpty(monitorHttp.Body))
            {
                Console.WriteLine($"Body: {monitorHttp.Body}");
                try
                {
                    JsonDocument.Parse(monitorHttp.Body); // Throws if invalid
                    content = new StringContent(monitorHttp.Body, System.Text.Encoding.UTF8, "application/json");
                }
                catch (JsonException err)
                {
                    // Log and reject
                    _logger.LogError("Invalid JSON input: {message}", err.Message);
                }
            }

            client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

            var sw = new Stopwatch();
            sw.Start();
            HttpResponseMessage? response = null;

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
        // catch if System.Net.Http.HttpRequestException
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError("HTTP Request error: {message}", httpRequestException.Message);
            client?.Dispose();
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ReasonPhrase = httpRequestException.Message
            };
        }
        catch (Exception err)
        {
            _logger.LogError("Error making HTTP call: {message}", err.Message);
            SentrySdk.CaptureException(err);
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

    public MonitorHttpHeaders CheckHttpHeaders(HttpResponseMessage response)
    {
        // Get the response headers
        var headers = response.Headers;

        var monitorHttpHeaders = new MonitorHttpHeaders
        {
            CacheControl = TryGetHeaderValue(headers, "Cache-Control"),
            StrictTransportSecurity = TryGetHeaderValue(headers, "Strict-Transport-Security"),
            PermissionsPolicy = TryGetHeaderValue(headers, "Permissions-Policy"),
            XFrameOptions = TryGetHeaderValue(headers, "X-Frame-Options"),
            XContentTypeOptions = TryGetHeaderValue(headers, "X-Content-Type-Options"),
            ReferrerPolicy = TryGetHeaderValue(headers, "Referrer-Policy"),
            ContentSecurityPolicy = TryGetHeaderValue(headers, "Content-Security-Policy")
        };

        return monitorHttpHeaders;
    }

    // Helper method to safely get header values
    private string? TryGetHeaderValue(HttpHeaders headers, string headerName)
    {
        if (headers.TryGetValues(headerName, out var values))
        {
            return values.FirstOrDefault();
        }
        return null; // Return null if the header is not found
    }
}