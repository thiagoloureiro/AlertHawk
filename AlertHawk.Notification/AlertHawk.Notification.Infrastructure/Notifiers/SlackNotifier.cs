using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class SlackNotifier : ISlackNotifier
    {
        public async Task SendNotification(string channel, string message, string webHookUrl)
        {
            using HttpClient httpClient = new HttpClient();
            string payload = $"{{\"channel\": \"{channel}\", \"text\": \"{message}\"}}";

            using StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Notification sent successfully!");
        }
    }
}