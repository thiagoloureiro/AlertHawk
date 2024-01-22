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

            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Notification sent successfully!");
        }
    }
}