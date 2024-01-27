using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using MassTransit;

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
        var webHookUrl = Environment.GetEnvironmentVariable("slack-webhookurl");

        await _slackNotifier.SendNotification("alerthawk-test", context.Message.Message, webHookUrl);
        // Handle the received message
        Console.WriteLine(
            $"NotificationId: {context.Message.NotificationId} Notification Message: {context.Message.Message}");
    }
}