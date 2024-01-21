using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using EasyMemoryCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AlertHawk.Notification.Tests.ControllerTests;

public class NotificationTypeControllerTests
{
    private readonly NotificationTypeController _controller;
    private readonly INotificationTypeService _notificationTypeService;
    private readonly ICaching _caching;

    public NotificationTypeControllerTests()
    {
        _notificationTypeService = Substitute.For<INotificationTypeService>();
        _caching = Substitute.For<ICaching>();
        _controller = new NotificationTypeController(_notificationTypeService, _caching);
    }

    [Fact]
    public async Task GetNotificationTypes_ReturnsOkResult()
    {
        // Arrange
        var notificationTypes = new List<NotificationType>();

        // Act
        var result = await _controller.GetNotificationTypes() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(notificationTypes, result.Value);
    }

    [Fact]
    public async Task GetNotificationTypesById_ExistingId_ReturnsOkResult()
    {
        // Arrange
        var existingId = 1; // Provide an existing id
        var notificationType = new NotificationType(); // Provide sample data
        _notificationTypeService.SelectNotificationTypeById(existingId).Returns(notificationType);

        // Act
        var result = await _controller.GetNotificationTypesById(existingId) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(notificationType, result.Value);
    }

    [Fact]
    public async Task GetNotificationTypesById_NonExistingId_ReturnsNotFoundResult()
    {
        // Arrange
        var nonExistingId = 999; // Provide a non-existing id
        _notificationTypeService.SelectNotificationTypeById(nonExistingId).Returns((NotificationType)null!);

        // Act
        var result = await _controller.GetNotificationTypesById(nonExistingId) as NotFoundResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task InsertNotificationType_ValidData_ReturnsOkResult()
    {
        // Arrange
        var notification = new NotificationType { Name = "NewNotification" }; // Provide valid data
        _notificationTypeService.SelectNotificationTypeByName(notification.Name).Returns((NotificationType)null!);

        // Act
        var result = await _controller.InsertNotificationType(notification) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal("Item successfully created", result.Value);
        await _notificationTypeService.Received(1).InsertNotificationType(notification);
        _caching.Received(1).Invalidate("GetNotificationTypes");
    }

    [Fact]
    public async Task InsertNotificationType_DuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var duplicateName = "ExistingNotification";
        var notification = new NotificationType { Name = duplicateName };
        _notificationTypeService.SelectNotificationTypeByName(duplicateName).Returns(notification);

        // Act
        var result = await _controller.InsertNotificationType(notification) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("already exists", result.Value?.ToString());
        await _notificationTypeService.DidNotReceive().InsertNotificationType(Arg.Any<NotificationType>());
        _caching.DidNotReceive().Invalidate("GetNotificationTypes");
    }

    [Fact]
    public async Task UpdateNotificationType_ValidData_ReturnsOkResult()
    {
        // Arrange
        var notification = new NotificationType { Id = 1, Name = "UpdatedNotification" }; // Provide valid data

        // Act
        var result = await _controller.UpdateNotificationType(notification) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal("Item successfully updated", result.Value);
        await _notificationTypeService.Received(1).UpdateNotificationType(notification);
        _caching.Received(1).Invalidate("GetNotificationTypes");
    }

    [Fact]
    public async Task DeleteNotificationType_ExistingId_ReturnsOkResult()
    {
        // Arrange
        var existingId = 1; // Provide an existing id
        var notificationTypeById = new NotificationType { Id = existingId };
        _notificationTypeService.SelectNotificationTypeById(existingId).Returns(notificationTypeById);

        // Act
        var result = await _controller.DeleteNotificationType(existingId) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal("Item successfully deleted", result.Value);
        await _notificationTypeService.Received(1).DeleteNotificationType(existingId);
        _caching.Received(1).Invalidate("GetNotificationTypes");
    }

    [Fact]
    public async Task DeleteNotificationType_NonExistingId_ReturnsNotFoundResult()
    {
        // Arrange
        var nonExistingId = 999; // Provide a non-existing id
        _notificationTypeService.SelectNotificationTypeById(nonExistingId).Returns((NotificationType)null!);

        // Act
        var result = await _controller.DeleteNotificationType(nonExistingId) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        await _notificationTypeService.DidNotReceive().DeleteNotificationType(Arg.Any<int>());
        _caching.DidNotReceive().Invalidate("GetNotificationTypes");
    }
}