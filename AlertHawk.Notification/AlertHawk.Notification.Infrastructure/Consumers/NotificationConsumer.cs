using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using Rebus.Handlers;
using System.Diagnostics.CodeAnalysis;

namespace SharedModels;

[ExcludeFromCodeCoverage]
public class NotificationConsumer : IHandleMessages<NotificationAlertMessage>
{
    private readonly INotificationService _notificationService;

    public NotificationConsumer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(NotificationAlertMessage message)
    {
        Console.WriteLine($"Received from message bus, " +
                          $"Message: {message.Message} " +
                          $"NotificationId: {message.NotificationId}" +
                          $"TimeStamp: {message.TimeStamp}");

        var notificationItem = await _notificationService.SelectNotificationItemById(message.NotificationId);

        if (notificationItem?.NotificationEmail != null)
        {
            Console.WriteLine("Sending Email notification");
            var notificationSend = new NotificationSend
            {
                NotificationEmail = notificationItem.NotificationEmail,
                Message = message.Message,
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
                Message = message.Message,
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
                Message = message.Message,
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
                Message = message.Message,
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
                Message = message.Message,
                NotificationTypeId = notificationItem.NotificationTypeId,
                MonitorId = message.MonitorId,
                Service = message.Service,
                Region = message.Region,
                Environment = message.Environment,
                StatusCode = message.StatusCode,
                ReasonPhrase = message.ReasonPhrase,
                URL = message.URL,
                Success = message.Success,
                NotificationTimeStamp = message.TimeStamp,
            };

            await _notificationService.Send(notificationSend);
        }

        if (notificationItem?.NotificationPush != null)
        {
            var deviceTokenList = await _notificationService.GetDeviceTokenList(notificationItem.MonitorGroupId);

            foreach (var token in deviceTokenList)
            {
                Console.WriteLine("Sending Push notification to device token: " + token);

                notificationItem.NotificationPush.PushNotificationBody = new PushNotificationBody
                {
                    to = token,
                    data = new PushNotificationData
                    {
                        message = message.Message
                    },
                    notification = new PushNotificationItem
                    {
                        title = "AlertHawk Notification",
                        body = message.Message,
                        badge = 1,
                        sound = "ping.aiff"
                    }
                };

                var notificationSend = new NotificationSend
                {
                    NotificationPush = notificationItem.NotificationPush,
                    Message = message.Message,
                    NotificationTypeId = notificationItem.NotificationTypeId,
                };
                await _notificationService.Send(notificationSend);
            }
        }

        Console.WriteLine(
            $"NotificationId: {message.NotificationId} Notification Message: {message.Message}");
    }
}