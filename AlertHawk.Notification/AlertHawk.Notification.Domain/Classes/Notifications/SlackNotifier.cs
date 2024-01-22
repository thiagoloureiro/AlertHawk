using System.Text;

namespace AlertHawk.Notification.Domain.Classes.Notifications
{
    public static class SlackNotifier
    {
        public static string webHookUrl = "";

        public static async Task SendNotification(string channel, string message)
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