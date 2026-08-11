using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class HttpClientRunner : IHttpClientRunner
{
    public const string DefaultHttpClientName = "AlertHawk.MonitorHttp";
    public const string InsecureHttpClientName = "AlertHawk.MonitorHttp.Insecure";

    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpClientRunner> _logger;
    private int _daysToExpireCert;
    private readonly int _retryIntervalMilliseconds = 6000;

    public HttpClientRunner(IMonitorRepository monitorRepository,
        INotificationProducer notificationProducer, IMonitorAlertRepository monitorAlertRepository,
        IMonitorHistoryRepository monitorHistoryRepository, ISystemConfigurationRepository systemConfigurationRepository,
        IHttpClientFactory httpClientFactory, ILogger<HttpClientRunner> logger)
    {
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
        _monitorHistoryRepository = monitorHistoryRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _retryIntervalMilliseconds = Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS") != null
            ? int.Parse(Environment.GetEnvironmentVariable("HTTP_RETRY_INTERVAL_MS")!)
            : 6000;
    }

    public async Task CheckUrlsAsync(MonitorHttp monitorHttp)
    {
        // Check if monitor execution is disabled (system maintenance mode)
        if (await _systemConfigurationRepository.IsMonitorExecutionDisabled())
        {
            _logger.LogInformation("Monitor execution is disabled. Skipping HTTP monitor check for MonitorId: {MonitorId}", monitorHttp.MonitorId);
            return;
        }
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

                if (monitorHttp.CheckCertExpiry && _daysToExpireCert <= 0 && succeeded)
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
                    await Task.Delay(_retryIntervalMilliseconds);

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

                            _logger.LogInformation("Saving monitor alert for {monitorHttp.UrlToCheck}", monitorHttp.UrlToCheck);

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

                await Task.Delay(_retryIntervalMilliseconds);
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    public async Task<HttpResponseMessage> MakeHttpClientCall(MonitorHttp monitorHttp)
    {
        // CheckCertExpiry needs a handler that can capture expiry days via a local (Ssl callbacks
        // do not reliably share AsyncLocal with the calling async flow). All other paths use
        // IHttpClientFactory so connections are pooled across checks/retries.
        if (monitorHttp.CheckCertExpiry)
        {
            return await MakeHttpClientCallWithCertExpiryCheck(monitorHttp);
        }

        var clientName = monitorHttp.IgnoreTlsSsl
            ? InsecureHttpClientName
            : DefaultHttpClientName;

        var client = _httpClientFactory.CreateClient(clientName);
        client.Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout);

        try
        {
            return await SendRequestAsync(client, monitorHttp);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError("HTTP Request error: {message}", httpRequestException.Message);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ReasonPhrase = SanitizeReasonPhrase(httpRequestException.Message)
            };
        }
        catch (Exception err)
        {
            _logger.LogError("Error making HTTP call: {message}", err.Message);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = "Internal Server Error"
            };
        }
    }

    private async Task<HttpResponseMessage> MakeHttpClientCallWithCertExpiryCheck(MonitorHttp monitorHttp)
    {
        var daysToExpire = 0;
        var maxRedirects = monitorHttp.MaxRedirects is > 0 and <= 50 ? monitorHttp.MaxRedirects : 50;

        using var handler = new HttpClientHandler
        {
            MaxAutomaticRedirections = maxRedirects,
            AllowAutoRedirect = maxRedirects > 0,
            // CheckCertExpiry previously always accepted the cert (callback returned true).
            ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
            {
                if (cert != null)
                {
                    daysToExpire = (cert.NotAfter - DateTime.UtcNow).Days;
                }

                return true;
            }
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(monitorHttp.Timeout)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AlertHawk/1.0.1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "br");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");

        try
        {
            var response = await SendRequestAsync(client, monitorHttp);
            _daysToExpireCert = daysToExpire;
            return response;
        }
        catch (HttpRequestException httpRequestException)
        {
            _daysToExpireCert = daysToExpire;
            _logger.LogError("HTTP Request error: {message}", httpRequestException.Message);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ReasonPhrase = SanitizeReasonPhrase(httpRequestException.Message)
            };
        }
        catch (Exception err)
        {
            _daysToExpireCert = daysToExpire;
            _logger.LogError("Error making HTTP call: {message}", err.Message);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = "Internal Server Error"
            };
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpClient client, MonitorHttp monitorHttp)
    {
        using var request = CreateHttpRequest(monitorHttp);

        var sw = Stopwatch.StartNew();
        var response = await client.SendAsync(request);
        sw.Stop();

        monitorHttp.ResponseTime = (int)sw.ElapsedMilliseconds;
        monitorHttp.ResponseStatusCode = response.StatusCode;
        monitorHttp.HttpVersion = response.Version.ToString();
        return response;
    }

    private HttpRequestMessage CreateHttpRequest(MonitorHttp monitorHttp)
    {
        var method = monitorHttp.MonitorHttpMethod switch
        {
            MonitorHttpMethod.Get => HttpMethod.Get,
            MonitorHttpMethod.Post => HttpMethod.Post,
            MonitorHttpMethod.Put => HttpMethod.Put,
            _ => throw new ArgumentOutOfRangeException()
        };

        var request = new HttpRequestMessage(method, monitorHttp.UrlToCheck);

        if (!string.IsNullOrEmpty(monitorHttp.Body) && method != HttpMethod.Get)
        {
            try
            {
                JsonDocument.Parse(monitorHttp.Body); // Throws if invalid
                request.Content = new StringContent(monitorHttp.Body, System.Text.Encoding.UTF8, "application/json");
            }
            catch (JsonException err)
            {
                _logger.LogError("Invalid JSON input: {message}", err.Message);
            }
        }

        if (monitorHttp.Headers != null)
        {
            foreach (var header in monitorHttp.Headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Item1, header.Item2))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                }
            }
        }

        return request;
    }

    /// <summary>
    /// SSL callback for the shared insecure named client (IgnoreTlsSsl path).
    /// </summary>
    public static bool InsecureCertificateValidationCallback(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors) => true;

    private static string SanitizeReasonPhrase(string message)
    {
        // HttpResponseMessage.ReasonPhrase rejects CR/LF and very long values.
        if (string.IsNullOrEmpty(message))
        {
            return "Request failed";
        }

        var sanitized = message.Replace('\r', ' ').Replace('\n', ' ');
        return sanitized.Length > 512 ? sanitized[..512] : sanitized;
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
