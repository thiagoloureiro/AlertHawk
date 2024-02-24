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
        var notificationItem = new NotificationItem { NotificationTypeId = typeId }; // Example notification type
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
}