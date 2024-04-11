using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Tests.NotifierTests;

public class NotifierTests : IClassFixture<NotificationController>
{
    private readonly IMailNotifier _mailNotifier;
    private readonly ISlackNotifier _slackNotifier;
    private readonly ITeamsNotifier _teamsNotifier;
    private readonly ITelegramNotifier _telegramNotifier;
    private readonly IWebHookNotifier _webHookNotifier;

    public NotifierTests(IMailNotifier mailNotifier, ISlackNotifier slackNotifier, ITeamsNotifier teamsNotifier, ITelegramNotifier telegramNotifier, IWebHookNotifier webHookNotifier)
    {
        _mailNotifier = mailNotifier;
        _slackNotifier = slackNotifier;
        _teamsNotifier = teamsNotifier;
        _telegramNotifier = telegramNotifier;
        _webHookNotifier = webHookNotifier;
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
        var result = await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId, notificationSend.Message, notificationSend.NotificationTelegram.TelegramBotToken);

        // Assert
        Assert.NotNull(result);
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
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _mailNotifier.Send(notificationSend.NotificationEmail);

        // Assert
        Assert.NotNull(result);
        Assert.True(result);
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
        var result = await _mailNotifier.Send(notificationSend.NotificationEmail);

        // Assert
        Assert.True(result);
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
        await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel, notificationSend.Message, notificationSend.NotificationSlack.WebHookUrl);

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
    public async Task Should_Send_WebHook_Notification()
    {
        // Arrange
        var message = "Message Details from Webhook";
        var channel = "alerthawk-test";

        var body = $"{{\"channel\": \"{channel}\", \"text\": \"{message}\"}}";
        var headers = "{User-Agent: \"Mozilla/5.0\"}";

        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 5, // WebHook
            NotificationWebHook = new NotificationWebHook()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.SlackWebHookUrl,
                Message = "test From WebHook Test",
                Body = body,
                HeadersJson = headers
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        await _webHookNotifier.SendNotification(notificationSend.Message, notificationSend.NotificationWebHook.WebHookUrl, notificationSend.NotificationWebHook.Body, notificationSend.NotificationWebHook.Headers);

        // Assert
        Assert.True(true);
    }
}