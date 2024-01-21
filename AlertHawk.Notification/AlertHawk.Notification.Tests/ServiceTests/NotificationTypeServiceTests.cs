using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using NSubstitute;

namespace AlertHawk.Notification.Tests.ServiceTests;

public class NotificationTypeServiceTests
{
    [Fact]
    public async Task SelectNotificationType_ShouldReturnNotificationTypes()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);

        var expectedNotificationTypes = new List<NotificationType>
        {
            new NotificationType { Id = 1, Name = "Type1", Description = "Item Description1"},
            new NotificationType { Id = 2, Name = "Type2", Description = "Item Description2"}
        };

        repositorySubstitute.SelectNotificationType().Returns(expectedNotificationTypes);

        // Act
        var result = await service.SelectNotificationType();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedNotificationTypes, result);
    }

    [Fact]
    public async Task SelectNotificationTypeById_ShouldReturnNotificationType()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);

        var expectedNotificationType = new NotificationType { Id = 1, Name = "Type1", Description = "Item Description1" };

        repositorySubstitute.SelectNotificationTypeById(Arg.Any<int>()).Returns(expectedNotificationType);

        // Act
        var result = await service.SelectNotificationTypeById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedNotificationType, result);
    }

    [Fact]
    public async Task SelectNotificationTypeByName_ShouldReturnNotificationType()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);

        var expectedNotificationType = new NotificationType { Id = 1, Name = "Type1", Description = "Item Description1" };

        repositorySubstitute.SelectNotificationTypeByName(Arg.Any<string>()).Returns(expectedNotificationType);

        // Act
        var result = await service.SelectNotificationTypeByName("Type1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedNotificationType, result);
    }

    [Fact]
    public async Task InsertNotificationType_ShouldCallRepositoryMethod()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);
        var notificationType = new NotificationType { Id = 1, Name = "Type1", Description = "Item Description1" };

        // Act
        await service.InsertNotificationType(notificationType);

        // Assert
        await repositorySubstitute.Received(1).InsertNotificationType(notificationType);
    }

    [Fact]
    public async Task UpdateNotificationType_ShouldCallRepositoryMethod()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);
        var notificationType = new NotificationType { Id = 1, Name = "Type1", Description = "Item Description1" };

        // Act
        await service.UpdateNotificationType(notificationType);

        // Assert
        await repositorySubstitute.Received(1).UpdateNotificationType(notificationType);
    }

    [Fact]
    public async Task DeleteNotificationType_ShouldCallRepositoryMethod()
    {
        // Arrange
        var repositorySubstitute = Substitute.For<INotificationTypeRepository>();
        var service = new NotificationTypeService(repositorySubstitute);

        // Act
        await service.DeleteNotificationType(1);

        // Assert
        await repositorySubstitute.Received(1).DeleteNotificationType(1);
    }
}