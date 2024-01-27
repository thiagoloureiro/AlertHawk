using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Services;
using MassTransit;

namespace SharedModels;

[ExcludeFromCodeCoverage]
public class NotificationConsumer : IConsumer<NotificationAlert>
{
    private readonly ISlackNotifier _slackNotifier;
    private readonly INotificationService _notificationService;
    public NotificationConsumer(ISlackNotifier slackNotifier, INotificationService notificationService)
    {
        _slackNotifier = slackNotifier;
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<NotificationAlert> context)
    {
        var notificationItem = await _notificationService.SelectNotificationItemById(context.Message.NotificationId);

        if (notificationItem?.NotificationEmail != null)
        {
            var notificationSend = new NotificationSend
            {
                NotificationEmail = notificationItem.NotificationEmail,
                Message = context.Message.Message
            };
            await _notificationService.Send(notificationSend);
        }
        
        if (notificationItem?.NotificationTeams != null)
        {
            var notificationSend = new NotificationSend
            {
                NotificationTeams = notificationItem.NotificationTeams,
                Message = context.Message.Message,
            };
            await _notificationService.Send(notificationSend);
        }
        
        if (notificationItem?.NotificationSlack != null)
        {
            var notificationSend = new NotificationSend
            {
                NotificationSlack = notificationItem.NotificationSlack,
                Message = context.Message.Message,
            };
            await _notificationService.Send(notificationSend);
        }
        
        if (notificationItem?.NotificationTelegram != null)
        {
            var notificationSend = new NotificationSend
            {
                NotificationTelegram = notificationItem.NotificationTelegram,
                Message = context.Message.Message,
            };
            await _notificationService.Send(notificationSend);
        }
        
        // Handle the received message
        Console.WriteLine(
            $"NotificationId: {context.Message.NotificationId} Notification Message: {context.Message.Message}");
    }
}