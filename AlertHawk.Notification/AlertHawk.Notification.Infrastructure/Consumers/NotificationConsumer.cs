using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using MassTransit;
using Sentry;

namespace SharedModels;

[ExcludeFromCodeCoverage]
public class NotificationConsumer : IConsumer<NotificationAlert>
{
    private readonly ISlackNotifier _slackNotifier;

    public NotificationConsumer(ISlackNotifier slackNotifier)
    {
        _slackNotifier = slackNotifier;
    }

    public async Task Consume(ConsumeContext<NotificationAlert> context)
    {
        // Later Fetch webhookUrl from NotificationId
        var webHookUrl = Environment.GetEnvironmentVariable("slack-webhookurl");

        if (webHookUrl != null)
        {
            await _slackNotifier.SendNotification("alerthawk-test", context.Message.Message, webHookUrl);
        }
        
        // Handle the received message
        Console.WriteLine(
            $"NotificationId: {context.Message.NotificationId} Notification Message: {context.Message.Message}");
    }
}