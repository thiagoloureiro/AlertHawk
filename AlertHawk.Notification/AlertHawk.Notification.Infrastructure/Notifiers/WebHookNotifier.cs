using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using StringContent = System.Net.Http.StringContent;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class WebHookNotifier : IWebHookNotifier
    {
        public async Task SendNotification(string message, string webHookUrl, string? body,
            List<Tuple<string, string>> headers)
        {
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

            var response = await httpClient.PostAsync(webHookUrl, content);
            
            response.EnsureSuccessStatusCode();
        }
    }
}