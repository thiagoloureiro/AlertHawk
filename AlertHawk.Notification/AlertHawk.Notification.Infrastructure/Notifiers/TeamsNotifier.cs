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

            // Check if message indicates healthy or has issues
            // For node metrics: "is healthy" or "has issues"
            // For monitor notifications: "Success" or "Error"
            var isHealthy = message.Contains("healthy", StringComparison.OrdinalIgnoreCase) || 
                           message.Contains("Success", StringComparison.OrdinalIgnoreCase);
            var hasIssues = message.Contains("has issues", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("Error", StringComparison.OrdinalIgnoreCase);
            
            message = isHealthy 
                ? "\\n\u2705 " + message
                : (hasIssues ? "\\n\u274c " + message : message);

            var payloadObj = new { text = message };

            string jsonPayload = JsonSerializer.Serialize(payloadObj);

            StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(webHookUrl, content);

            response.EnsureSuccessStatusCode();
        }
    }
}