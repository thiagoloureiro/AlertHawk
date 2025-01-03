using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace SharedModels;

[ExcludeFromCodeCoverage]
public class NotificationConsumer : IConsumer<NotificationAlert>
{
    private readonly INotificationService _notificationService;

    public NotificationConsumer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<NotificationAlert> context)
    {
        Console.WriteLine($"Received from RabbitMq, " +
                          $"Message: {context.Message.Message} " +
                          $"NotificationId: {context.Message.NotificationId}" +
                          $"TimeStamp: {context.Message.TimeStamp}");

        var notificationItem = await _notificationService.SelectNotificationItemById(context.Message.NotificationId);

        if (notificationItem?.NotificationEmail != null)
        {
            Console.WriteLine("Sending Email notification");
            var notificationSend = new NotificationSend
            {
                NotificationEmail = notificationItem.NotificationEmail,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId,
            };
            await _notificationService.Send(notificationSend);
        }

        if (notificationItem?.NotificationTeams != null)
        {
            Console.WriteLine("Sending Teams notification");
            var notificationSend = new NotificationSend
            {
                NotificationTeams = notificationItem.NotificationTeams,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId
            };
            await _notificationService.Send(notificationSend);
        }

        if (notificationItem?.NotificationSlack != null)
        {
            Console.WriteLine("Sending Slack notification");
            var notificationSend = new NotificationSend
            {
                NotificationSlack = notificationItem.NotificationSlack,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId
            };
            await _notificationService.Send(notificationSend);
        }

        if (notificationItem?.NotificationTelegram != null)
        {
            Console.WriteLine("Sending Telegram notification");
            var notificationSend = new NotificationSend
            {
                NotificationTelegram = notificationItem.NotificationTelegram,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId
            };
            await _notificationService.Send(notificationSend);
        }

        if (notificationItem?.NotificationWebHook != null)
        {
            Console.WriteLine("Sending WebHook notification");
            var notificationSend = new NotificationSend
            {
                NotificationWebHook = notificationItem.NotificationWebHook,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId
            };
            await _notificationService.Send(notificationSend);
        }
        
        if (notificationItem?.NotificationPush != null)
        {
            Console.WriteLine("Sending Push notification");
            var notificationSend = new NotificationSend
            {
                NotificationPush = notificationItem.NotificationPush,
                Message = context.Message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId
            };
            await _notificationService.Send(notificationSend);
        }

        // Handle the received message
        Console.WriteLine(
            $"NotificationId: {context.Message.NotificationId} Notification Message: {context.Message.Message}");
    }
}