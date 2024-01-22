using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Notification.Tests.NotifierTests;

public class NotifierTests : IClassFixture<NotificationController>
{
    private NotificationController _notificationController;

    public NotifierTests(NotificationController notificationController)
    {
        _notificationController = notificationController;
    }

    [Fact]
    public async Task Should_Send_Telegram_Notification()
    {
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 3,
            NotificationTelegram = new NotificationTelegram
            {
                ChatId = Convert.ToInt64(Environment.GetEnvironmentVariable("ChatId")),
                NotificationId = 1,
                TelegramBotToken = Environment.GetEnvironmentVariable("TelegramBotToken"),
            },

        };
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
}