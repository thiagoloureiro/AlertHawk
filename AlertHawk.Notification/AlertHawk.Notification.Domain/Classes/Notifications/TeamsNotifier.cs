using System.Text;

namespace AlertHawk.Notification.Domain.Classes.Notifications
{
    public static class TeamsNotifier
    {
        public static async Task SendNotification(string message)
        {
            try
            {
                var TeamsWebhookUrl = "";
                using (HttpClient httpClient = new HttpClient())
                {
                    string payload = $"{{\"text\": \"{message}\"}}";

                    StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await httpClient.PostAsync(TeamsWebhookUrl, content);

                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("Notification sent successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}