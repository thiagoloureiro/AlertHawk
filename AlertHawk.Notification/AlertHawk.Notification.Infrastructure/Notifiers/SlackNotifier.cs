using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using Sentry;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class SlackNotifier : ISlackNotifier
    {
        public async Task SendNotification(string channel, string message, string webHookUrl)
        {
            try
            {
                Console.WriteLine(
                    $"Sending notification channel: {channel}, message: {message} webhookUrl: {webHookUrl}");
                using HttpClient httpClient = new HttpClient();
                string payload = $"{{\"channel\": \"{channel}\", \"text\": \"{message}\"}}";

                Console.WriteLine(payload);
                using StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(response.Content.ReadAsStringAsync());
                }
                response.EnsureSuccessStatusCode();

                Console.WriteLine("Notification sent successfully!");
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        }
    }
}