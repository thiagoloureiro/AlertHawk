using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AlertHawk.Notification.Tests.ControllerTests
{
    public class NotificationControllerTests
    {
        private readonly INotificationService _notificationService;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _notificationService = Substitute.For<INotificationService>();
            _controller = new NotificationController(_notificationService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task InsertNotificationItem_ShouldReturnOk_WhenNotificationItemIsCreated()
        {
            // Arrange
            var notificationItem = new NotificationItem();
            _notificationService.InsertNotificationItem(notificationItem).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.InsertNotificationItem(notificationItem) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal("Notification Created Successfully", result.Value);
        }

        [Fact]
        public async Task UpdateNotificationItem_ShouldReturnOk_WhenNotificationItemIsUpdated()
        {
            // Arrange
            var notificationItem = new NotificationItem();
            _notificationService.UpdateNotificationItem(notificationItem).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateNotificationItem(notificationItem) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal("Notification Updated Successfully", result.Value);
        }

        [Fact]
        public async Task DeleteNotificationItem_ShouldReturnOk_WhenNotificationItemIsDeleted()
        {
            // Arrange
            var id = 1;
            _notificationService.DeleteNotificationItem(id).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteNotificationItem(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal("Notification Deleted Successfully", result.Value);
        }

        [Fact]
        public async Task SelectNotificationItemById_ShouldReturnOk_WithNotificationItem()
        {
            // Arrange
            var id = 1;
            var notificationItem = new NotificationItem
            {
                NotificationEmail = new NotificationEmail { FromEmail = "user@user.com", Password = "password" }
            };
            _notificationService.SelectNotificationItemById(id).Returns(notificationItem);

            // Act
            var result = await _controller.SelectNotificationItemList(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            var item = Assert.IsType<NotificationItem>(result.Value);
            Assert.Equal("", item.NotificationEmail?.Password); // Password should be cleared
        }

        [Fact]
        public async Task SelectNotificationItemListByIds_ShouldReturnOk_WithNotificationItems()
        {
            // Arrange
            var ids = new List<int> { 1, 2, 3 };
            var notificationItems = new List<NotificationItem>
            {
                new NotificationItem { NotificationEmail = new NotificationEmail {FromEmail = "user@user.com", Password = "password" } }
            };
            _notificationService.SelectNotificationItemList(ids).Returns(notificationItems);

            // Act
            var result = await _controller.SelectNotificationItemList(ids) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            var items = Assert.IsType<List<NotificationItem>>(result.Value);
            Assert.Single(items);
            Assert.Equal("", items[0].NotificationEmail?.Password); // Password should be cleared
        }

        [Fact]
        public async Task SelectNotificationItemByMonitorGroupId_ShouldReturnOk_WithNotificationItems()
        {
            // Arrange
            var id = 1;
            var notificationItems = new List<NotificationItem>
            {
                new NotificationItem { NotificationEmail = new NotificationEmail { FromEmail = "user@user.com", Password = "password" } }
            };
            _notificationService.SelectNotificationItemByMonitorGroupId(id).Returns(notificationItems);

            // Act
            var result = await _controller.SelectNotificationItemByMonitorGroupId(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            var items = Assert.IsType<List<NotificationItem>>(result.Value);
            Assert.Single(items);
        }

        [Fact]
        public async Task GetNotificationCount_ShouldReturnOk_WithNotificationCount()
        {
            // Arrange
            var count = 10;
            _notificationService.GetNotificationLogCount().Returns(count);

            // Act
            var result = await _controller.GetNotificationCount() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(count, Convert.ToInt32(result.Value));
        }

        [Fact]
        public async Task GetNotificationCount_ShouldReturnOk_WithZeroNotificationCount()
        {
            // Arrange
            _notificationService.GetNotificationLogCount().Returns(0);

            // Act
            var result = await _controller.GetNotificationCount() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(0, Convert.ToInt32(result.Value));
        }

        [Fact]
        public async Task SelectNotificationItemByMonitorGroupId_ShouldReturnOk_WithEmptyNotificationItems()
        {
            // Arrange
            var id = 1;
            var notificationItems = new List<NotificationItem>();
            _notificationService.SelectNotificationItemByMonitorGroupId(id).Returns(notificationItems);

            // Act
            var result = await _controller.SelectNotificationItemByMonitorGroupId(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            var items = Assert.IsType<List<NotificationItem>>(result.Value);
            Assert.Empty(items);
        }

        [Fact]
        public async Task SendNotification_ShouldReturnOk_WhenNotificationIsSent()
        {
            // Arrange
            var keys = Utils.GenerateAesKeyAndIv();
            Environment.SetEnvironmentVariable("AesKey", keys.Item1);
            Environment.SetEnvironmentVariable("AesIV", keys.Item2);

            var notificationItem = new NotificationSend
            {
                Message = "Message",
                NotificationEmail = new NotificationEmail
                {
                    FromEmail = "user@user.com",
                    Password = "Password"
                }
            };
            _notificationService.Send(notificationItem).Returns(true);

            // Act
            var result = await _controller.SendNotification(notificationItem) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async Task SelectNotificationItemList_ShouldReturnOk_WithEmptyNotificationItems()
        {
            // Arrange
            var ids = new List<int> { 1, 2, 3 };
            var notificationItems = new List<NotificationItem>();
            
            _notificationService.SelectNotificationItemList(ids).Returns(notificationItems);

            // Act
            var result = await _controller.SelectNotificationItemList(ids) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            var items = Assert.IsType<List<NotificationItem>>(result.Value);
            Assert.Empty(items);
        }

        [Fact]
        public async Task SelectNotificationItemList_ShouldReturnOk()
        {
            // Arrange
            var notificationItems = new List<NotificationItem>
            {
                new NotificationItem
                {
                    Id = 1,
                    Description = "test",
                    Name = "Name",
                    NotificationEmail = new NotificationEmail
                    {
                        FromEmail = "user@user.com",
                        Password = "password",
                        ToEmail = "user@user.com",
                        EnableSsl = true,

                    }
                }
            };
            
            var token = "Bearer validJwtToken";
            _notificationService.SelectNotificationItemList(token).Returns(notificationItems);
            _controller.HttpContext.Request.Headers["Authorization"] = "Bearer validJwtToken";

            // Act
            var result = await _controller.SelectNotificationItemList() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }
        
        [Fact]
        public async Task SelectNotificationItemList_InvalidToken_ShouldReturnBadRequest()
        {
            // Arrange
            var notificationItems = new List<NotificationItem>();
            var token = "token";
            _notificationService.SelectNotificationItemList(token).Returns(notificationItems);
            _controller.HttpContext.Request.Headers["Authorization"] =  "invalidToken";

            // Act
            var result = await _controller.SelectNotificationItemList() as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result?.StatusCode);
        }
        
        [Fact]
        public async Task ClearNotificationStatistics_ShouldReturnOk()
        {
            // Arrange
            _notificationService.ClearNotificationStatistics().Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ClearNotificationStatistics() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }
    }
}