using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using Polly;
using System.Text;
using System.Text.Json;
using StringContent = System.Net.Http.StringContent;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    [ExcludeFromCodeCoverage]
    public class WebHookNotifier : IWebHookNotifier
    {
        public class WebHookRequest
        {
            public int NotificationId { get; set; }
            public int MonitorId { get; set; }
            public string Service { get; set; } = string.Empty;
            public int Region { get; set; }
            public int Environment { get; set; }
            public string URL { get; set; } = string.Empty;
            public int StatusCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ReasonPhrase { get; set; } = string.Empty;
            public string IP { get; set; } = string.Empty;
            public int Port { get; set; }
            public bool Success { get; set; }
            public string Body { get; set; } = string.Empty;
        }

        public async Task SendNotification(NotificationSend notification, NotificationWebHook webHook)
        {
            var headersString = string.Join(", ", webHook.Headers.Select(header => $"{header.Item1}: {header.Item2}"));

            Console.WriteLine($"Sending WebHook notification to {webHook.WebHookUrl} headers: {headersString} body: {webHook.Body}, message: {webHook.Message}");

            using HttpClient httpClient = new();

            StringContent? content = null;

            WebHookRequest request = new()
            {
                NotificationId = webHook.NotificationId,
                MonitorId = notification.MonitorId,
                Service = notification.Service,
                Region = notification.Region,
                Message = notification.Message,
                Environment = notification.Environment,
                URL = notification.URL,
                StatusCode = notification.StatusCode,
                ReasonPhrase = notification.ReasonPhrase,
                IP = notification.IP,
                Port = notification.Port,
                Success = notification.Success,
                Body = webHook.Body!
            };

            if (request != null)
            {
                var body = JsonSerializer.Serialize(request);
                content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            if (webHook.Headers != null)
            {
                var newHeaders = webHook.Headers;
                foreach (var header in newHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Item1, header.Item2);
                }
            }

            var policy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await policy.ExecuteAsync(async () =>
            {
                var response = await httpClient.PostAsync(webHook.WebHookUrl, content);
                response.EnsureSuccessStatusCode();
                return response;
            });
        }
    }
}