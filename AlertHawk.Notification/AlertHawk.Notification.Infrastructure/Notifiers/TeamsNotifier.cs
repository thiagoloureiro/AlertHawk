using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TeamsNotifier : ITeamsNotifier
    {
        public async Task SendNotification(string message, string webHookUrl)
        {
            using HttpClient httpClient = new HttpClient();
            
            message = message.Contains("Success") ? "\\n\u2705 " + message : "\\n\u274c " + message;
            
            string payload = $"{{\"text\": \"{message}\"}}";
       
            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();
        }
    }
}