using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Utils;

namespace AlertHawk.Notification.Tests.NotifierTests;

public class NotifierTests : IClassFixture<NotificationController>
{
    private readonly IMailNotifier _mailNotifier;
    private readonly ISlackNotifier _slackNotifier;
    private readonly ITeamsNotifier _teamsNotifier;
    private readonly ITelegramNotifier _telegramNotifier;
    private readonly IWebHookNotifier _webHookNotifier;
    private readonly IPushNotifier _pushNotifier;

    public NotifierTests(IMailNotifier mailNotifier, ISlackNotifier slackNotifier, ITeamsNotifier teamsNotifier,
        ITelegramNotifier telegramNotifier, IWebHookNotifier webHookNotifier, IPushNotifier pushNotifier)
    {
        _mailNotifier = mailNotifier;
        _slackNotifier = slackNotifier;
        _teamsNotifier = teamsNotifier;
        _telegramNotifier = telegramNotifier;
        _webHookNotifier = webHookNotifier;
        _pushNotifier = pushNotifier;
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
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId,
            notificationSend.Message, notificationSend.NotificationTelegram.TelegramBotToken);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Send_Success_Telegram_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Success",
            NotificationTypeId = 3, // Telegram
            NotificationTelegram = new NotificationTelegram
            {
                ChatId = GlobalVariables.TelegramChatId,
                NotificationId = 1,
                TelegramBotToken = GlobalVariables.TelegramWebHook,
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId,
            notificationSend.Message, notificationSend.NotificationTelegram.TelegramBotToken);

        // Assert
        Assert.NotNull(result);
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
                Channel = "alerthawk-test"
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel, notificationSend.Message,
            notificationSend.NotificationSlack.WebHookUrl);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Send_Success_Slack_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Success",
            NotificationTypeId = 4, // Slack
            NotificationSlack = new NotificationSlack()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.SlackWebHookUrl,
                Channel = "alerthawk-test"
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel, notificationSend.Message,
            notificationSend.NotificationSlack.WebHookUrl);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Send_Teams_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Test from Unit testing",
            NotificationTypeId = 2, // Teams
            NotificationTeams = new NotificationTeams()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.TeamsWebHookUrl
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        await _teamsNotifier.SendNotification(notificationSend.Message, notificationSend.NotificationTeams.WebHookUrl);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Send_Success_Teams_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Success Test from Unit testing",
            NotificationTypeId = 2, // Teams
            NotificationTeams = new NotificationTeams()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.TeamsWebHookUrl
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        await _teamsNotifier.SendNotification(notificationSend.Message, notificationSend.NotificationTeams.WebHookUrl);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Send_Push_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "from backend",
            NotificationTypeId = 6, // Push
            NotificationTimeStamp = DateTime.UtcNow,
            NotificationPush = new NotificationPush()
            {
                NotificationId = 1,
                PushNotificationBody = new PushNotificationBody()
                {
                    to = "dbc452fdb71374b10fb22b",
                    data = new PushNotificationData()
                    {
                        message = "message"
                    },
                    notification = new PushNotificationItem()
                    {
                        title = "title",
                        body = "body",
                        badge = 1,
                        sound = "ping.aiff"
                    }
                }
            }
        };

        Environment.SetEnvironmentVariable("PUSHY_API_KEY", GlobalVariables.PushyApiKey);

        // Act
        await _pushNotifier.SendNotification(notificationSend.Message, notificationSend.NotificationPush);

        // Assert
        Assert.True(true);
    }
}