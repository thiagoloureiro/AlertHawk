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
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 3, // Telegram
            NotificationTelegram = new NotificationTelegram
            {
                ChatId = GlobalVariables.TelegramChatId,
                NotificationId = 1,
                TelegramBotToken = GlobalVariables.TelegramWebHook,
            },
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_EmailSmtp_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 1, // EmailSmtp
            NotificationEmail = new NotificationEmail
            {
                ToEmail = "alerthawk@outlook.com",
                FromEmail = "alerthawk@outlook.com",
                Username = "alerthawk@outlook.com",
                Password = GlobalVariables.EmailPassword,
                Hostname = "smtp.office365.com",
                Body = "Body",
                Subject = "Subject",
                Port = 587,
                EnableSsl = true,
                IsHtmlBody = false,
                NotificationId = 1
            },
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_EmailSmtpWithCCAndBCC_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 1, // EmailSmtp
            NotificationEmail = new NotificationEmail
            {
                ToEmail = "alerthawk@outlook.com",
                ToCCEmail = "alerthawk@outlook.com",
                ToBCCEmail = "alerthawk@outlook.com",
                FromEmail = "alerthawk@outlook.com",
                Username = "alerthawk@outlook.com",
                Password = GlobalVariables.EmailPassword,
                Hostname = "smtp.office365.com",
                Body = "Body",
                Subject = "Subject",
                Port = 587,
                EnableSsl = true,
                IsHtmlBody = false,
                NotificationId = 1
            },
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_Slack_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 4, // Slack
            NotificationSlack = new NotificationSlack()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.SlackWebHookUrl,
                ChannelName = "alerthawk-test"
            },
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
}