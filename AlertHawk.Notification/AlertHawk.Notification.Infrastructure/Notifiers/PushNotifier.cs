using System.Text;
using System.Text.Json;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers;

public class PushNotifier: IPushNotifier
{
    public async Task SendNotification(string message, NotificationPush notificationPush)
    {
        var api_key= Environment.GetEnvironmentVariable("PUSHY_API_KEY");
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.pushy.me");
        
        // Check if message indicates healthy or has issues
        // For node metrics: "is healthy" or "has issues"
        // For monitor notifications: "Success" or "Error"
        var isHealthy = message.Contains("healthy", StringComparison.OrdinalIgnoreCase) || 
                       message.Contains("Success", StringComparison.OrdinalIgnoreCase);
        var hasIssues = message.Contains("has issues", StringComparison.OrdinalIgnoreCase) ||
                       message.Contains("Error", StringComparison.OrdinalIgnoreCase);
        
        // Determine emoji based on message content
        var emoji = isHealthy ? "\u2705 " : (hasIssues ? "\u274c " : "");
        
        notificationPush.PushNotificationBody.data.message = emoji + message;
        notificationPush.PushNotificationBody.notification.body = emoji + message;

        var body = JsonSerializer.Serialize(notificationPush.PushNotificationBody);
        
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync($"/push?api_key={api_key}", content);

        response.EnsureSuccessStatusCode();
    }
}