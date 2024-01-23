using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Notification.Tests.ControllerTests;

public class NotifierTests : IClassFixture<NotificationController>
{
    private readonly NotificationController _notificationController;

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
                ChatId = GlobalVariables.TelegramChatId,
                NotificationId = 1,
                TelegramBotToken = GlobalVariables.TelegramWebHook,
            },
        };
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
}