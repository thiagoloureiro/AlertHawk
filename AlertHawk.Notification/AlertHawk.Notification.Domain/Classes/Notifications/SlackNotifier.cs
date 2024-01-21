using System.Text;

namespace AlertHawk.Notification.Domain.Classes.Notifications
{
    public static class SlackNotifier
    {
        public static async Task SendNotification(string channel, string message)
        {
            try
            {
                // Fetch WebhookUrl
                var SlackWebhookUrl = "";

                using (HttpClient httpClient = new HttpClient())
                {
                    string payload = $"{{\"channel\": \"{channel}\", \"text\": \"{message}\"}}";

                    StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await httpClient.PostAsync(SlackWebhookUrl, content);

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