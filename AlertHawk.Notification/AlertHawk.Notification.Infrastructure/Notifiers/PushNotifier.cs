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
        
        var body = JsonSerializer.Serialize(notificationPush.PushNotificationBody);
        
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync($"/push?api_key={api_key}", content);

        response.EnsureSuccessStatusCode();
    }
}