using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TeamsNotifier : ITeamsNotifier
    {
        public async Task SendNotification(string message, string webHookUrl)
        {
            using HttpClient httpClient = new HttpClient();
            string payload = $"{{\"text\": \"{message}\"}}";
            
            message = message.Contains("Success") ? ":white_check_mark: " + message : ":x: " + message;
            
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();
        }
    }
}