using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using NSubstitute;

namespace AlertHawk.Notification.Tests.ServiceTests;

public class NotificationServiceTests
{
    [Fact]
    public async Task InsertNotificationItemSmtp_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService, 1);

        // Act
        await notificationService.InsertNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).InsertNotificationItemEmailSmtp(Arg.Is(notificationItem));
    }

    [Fact]
    public async Task InsertNotificationItemTelegram_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService, 3);

        // Act
        await notificationService.InsertNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).InsertNotificationItemTelegram(Arg.Is(notificationItem));
    }

    [Fact]
    public async Task InsertNotificationItemSlack_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService, 4);

        // Act
        await notificationService.InsertNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).InsertNotificationItemSlack(Arg.Is(notificationItem));
    }

    [Fact]
    public async Task InsertNotificationItemMSTeams_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService, 2);

        // Act
        await notificationService.InsertNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).InsertNotificationItemMsTeams(Arg.Is(notificationItem));
    }

    [Fact]
    public async Task InsertNotificationItemWebHook_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService, 5);

        // Act
        await notificationService.InsertNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).InsertNotificationItemWebHook(Arg.Is(notificationItem));
    }

    private static NotificationItem CreateMock(out INotificationRepository notificationRepository,
        out NotificationService notificationService, int typeId = 1)
    {
        var notificationItem = new NotificationItem
        {
            NotificationTypeId = typeId, Description = "Description", Name = "Notification Name",
            NotificationEmail = new NotificationEmail
            {
                FromEmail = "user@email.com",
                ToEmail = "user@email.com",
                ToBCCEmail = "bcc@email.com",
                ToCCEmail = "cc@email.com",
                Hostname = "smtp.office365.com",
                Username = "username",
                Password = "password",
                IsHtmlBody = true,
                Subject = "Subject",
                Body = "Body",
                Port = 587,
                EnableSsl = true,
                NotificationId = 1
            },
            NotificationSlack = new NotificationSlack
            {
                Channel = "channel",
                WebHookUrl = "webhook",
                NotificationId = 1
            },
            NotificationTeams = new NotificationTeams
            {
                NotificationId = 1,
                WebHookUrl = "webhook"
            },
            NotificationWebHook = new NotificationWebHook
            {
                NotificationId = 1,
                WebHookUrl = "webhook",
                Message = "message",
                Body = "body",
                Headers = new List<Tuple<string, string>>(),
                HeadersJson = "headers"
            },
            NotificationTelegram = new NotificationTelegram
            {
                NotificationId = 1,
                ChatId = 123123123123,
                TelegramBotToken = "token"
            }
        }; // Example notification type

        var mailNotifier = Substitute.For<IMailNotifier>();
        var slackNotifier = Substitute.For<ISlackNotifier>();
        var teamsNotifier = Substitute.For<ITeamsNotifier>();
        var telegramNotifier = Substitute.For<ITelegramNotifier>();
        notificationRepository = Substitute.For<INotificationRepository>();
        var webHookNotifier = Substitute.For<IWebHookNotifier>();

        notificationService = new NotificationService(
            mailNotifier,
            slackNotifier,
            teamsNotifier,
            telegramNotifier,
            notificationRepository,
            webHookNotifier
        );
        return notificationItem;
    }

    [Fact]
    public async Task UpdateNotificationItem_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService);

        // Act
        await notificationService.UpdateNotificationItem(notificationItem);

        // Assert
        await notificationRepository.Received(1).UpdateNotificationItem(Arg.Is(notificationItem));
    }

    [Fact]
    public async Task DeleteNotificationItem_Calls_Correct_Method()
    {
        // Arrange
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService);

        // Act
        await notificationService.DeleteNotificationItem(notificationItem.Id);

        // Assert
        await notificationRepository.Received(1).DeleteNotificationItem(Arg.Is(notificationItem.Id));
    }

    [Fact]
    public async Task SelectNotificationItemList_Calls_Repository_Method()
    {
        // Arrange
        var expectedNotificationList = new List<NotificationItem>();
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService);
        expectedNotificationList.Add(notificationItem);
        notificationRepository.SelectNotificationItemList().Returns(expectedNotificationList);

        // Act
        var result = await notificationService.SelectNotificationItemList("token");

        // Assert
        await notificationRepository.Received(1).SelectNotificationItemList();
        Assert.Same(expectedNotificationList, result);
    }

    [Fact]
    public async Task SelectNotificationItemListByIds_Calls_Repository_Method()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2, 3 };
        var expectedNotificationList = new List<NotificationItem>();
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService);
        expectedNotificationList.Add(notificationItem);
        notificationRepository.SelectNotificationItemList(ids).Returns(expectedNotificationList);

        // Act
        var result = await notificationService.SelectNotificationItemList(ids);

        // Assert
        await notificationRepository.Received(1).SelectNotificationItemList(ids);
        Assert.Same(expectedNotificationList, result);
    }

    [Fact]
    public async Task SelectNotificationItemById_Calls_Repository_Method()
    {
        // Arrange
        int id = 1;
        var notificationItem = CreateMock(out var notificationRepository, out var notificationService);
        notificationRepository.SelectNotificationItemById(id).Returns(notificationItem);

        // Act
        var result = await notificationService.SelectNotificationItemById(id);

        // Assert
        await notificationRepository.Received(1).SelectNotificationItemById(id);
        Assert.Same(notificationItem, result);
    }
}