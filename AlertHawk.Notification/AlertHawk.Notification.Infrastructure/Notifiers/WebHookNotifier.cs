﻿using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using Polly;
using System.Text;
using StringContent = System.Net.Http.StringContent;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class WebHookNotifier : IWebHookNotifier
    {
        public async Task SendNotification(string message, string webHookUrl, string? body,
            List<Tuple<string, string>>? headers)
        {
            var headersString = string.Join(", ", headers.Select(header => $"{header.Item1}: {header.Item2}"));
            Console.WriteLine($"Sending WebHook notification to {webHookUrl} headers: {headersString} body: {body}, message: {message}");

            using HttpClient httpClient = new HttpClient();

            StringContent? content = null;

            if (body != null)
            {
                content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            if (headers != null)
            {
                var newHeaders = headers;
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
                var response = await httpClient.PostAsync(webHookUrl, content);
                response.EnsureSuccessStatusCode();
                return response;
            });
        }
    }
}