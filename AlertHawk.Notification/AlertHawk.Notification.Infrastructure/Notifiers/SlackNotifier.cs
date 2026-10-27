using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using System.Text;
using System.Text.Json;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class SlackNotifier : ISlackNotifier
    {
        private readonly HttpClient? _httpClient;

        public SlackNotifier()
        {
        }

        public SlackNotifier(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendNotification(string channel, string message, string webHookUrl)
        {
            var httpClient = _httpClient ?? new HttpClient();

            // Check if message indicates healthy or has issues
            // For node metrics: "is healthy" or "has issues"
            // For monitor notifications: "Success" or "Error"
            var isHealthy = message.Contains("healthy", StringComparison.OrdinalIgnoreCase) || 
                           message.Contains("Success", StringComparison.OrdinalIgnoreCase);
            var hasIssues = message.Contains("has issues", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("Error", StringComparison.OrdinalIgnoreCase);
            
            message = isHealthy 
                ? ":white_check_mark: " + message
                : (hasIssues ? ":x: " + message : message);

            var payload = new
            {
                channel = channel,
                text = message
            };

            string jsonPayload = JsonSerializer.Serialize(payload);

            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();

            if (_httpClient is null)
            {
                httpClient.Dispose();
            }
        }
    }
}