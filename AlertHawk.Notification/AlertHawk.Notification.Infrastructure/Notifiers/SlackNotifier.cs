using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using System.Text;
using System.Text.Json;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class SlackNotifier : ISlackNotifier
    {
        public async Task SendNotification(string channel, string message, string webHookUrl)
        {
            using HttpClient httpClient = new HttpClient();

            message = message.Contains("Success")
                ? ":white_check_mark: " + message
                : ":x: " + message;

            var payload = new
            {
                channel = channel,
                text = message
            };

            string jsonPayload = JsonSerializer.Serialize(payload);

            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();
        }
    }
}