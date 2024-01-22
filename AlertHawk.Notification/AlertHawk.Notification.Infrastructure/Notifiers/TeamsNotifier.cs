using System.Text;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TeamsNotifier : ITeamsNotifier
    {
        public async Task SendNotification(string message)
        {
            var TeamsWebhookUrl = "";
            using HttpClient httpClient = new HttpClient();
            string payload = $"{{\"text\": \"{message}\"}}";

            StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(TeamsWebhookUrl, content);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Notification sent successfully!");
        }
    }
}