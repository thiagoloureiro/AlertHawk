using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using System.Text;
using System.Text.Json;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TeamsNotifier : ITeamsNotifier
    {
        public async Task SendNotification(string message, string webHookUrl)
        {
            using HttpClient httpClient = new HttpClient();

            message = message.Contains("Success") ? "\\n\u2705 " + message : "\\n\u274c " + message;

            var payloadObj = new { text = message };

            string jsonPayload = JsonSerializer.Serialize(payloadObj);

            StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();
        }
    }
}