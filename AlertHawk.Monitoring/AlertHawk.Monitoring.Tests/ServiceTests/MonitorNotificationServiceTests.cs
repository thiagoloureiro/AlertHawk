using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Moq;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.ServiceTests
{
    public class MonitorNotificationServiceTests
    {
        private readonly Mock<IMonitorNotificationRepository> _monitorNotificationRepositoryMock;
        private readonly Mock<IMonitorGroupService> _monitorGroupServiceMock;
        private readonly IMonitorNotificationService _monitorNotificationService;

        public MonitorNotificationServiceTests()
        {
            _monitorNotificationRepositoryMock = new Mock<IMonitorNotificationRepository>();
            _monitorGroupServiceMock = new Mock<IMonitorGroupService>();
            _monitorNotificationService = new MonitorNotificationService(_monitorGroupServiceMock.Object,
                _monitorNotificationRepositoryMock.Object);
        }

        [Fact]
        public async Task GetMonitorNotifications_ReturnsNotifications()
        {
            // Arrange
            var notifications = new List<MonitorNotification> { new MonitorNotification() };
            _monitorNotificationRepositoryMock.Setup(repo => repo.GetMonitorNotifications(It.IsAny<int>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _monitorNotificationService.GetMonitorNotifications(1);

            // Assert
            Assert.Equal(notifications, result);
        }

        [Fact]
        public async Task AddMonitorNotification_AddsNotification()
        {
            // Arrange
            var notification = new MonitorNotification();
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.AddMonitorNotification(It.IsAny<MonitorNotification>()))
                .Returns(Task.CompletedTask);

            // Act
            await _monitorNotificationService.AddMonitorNotification(notification);

            // Assert
            _monitorNotificationRepositoryMock.Verify(repo => repo.AddMonitorNotification(notification), Times.Once);
        }

        [Fact]
        public async Task RemoveMonitorNotification_RemovesNotification()
        {
            // Arrange
            var notification = new MonitorNotification();
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.RemoveMonitorNotification(It.IsAny<MonitorNotification>()))
                .Returns(Task.CompletedTask);

            // Act
            await _monitorNotificationService.RemoveMonitorNotification(notification);

            // Assert
            _monitorNotificationRepositoryMock.Verify(repo => repo.RemoveMonitorNotification(notification), Times.Once);
        }

        [Fact]
        public async Task AddMonitorGroupNotification_AddsGroupNotification()
        {
            // Arrange
            var monitorGroupNotification = new MonitorGroupNotification { MonitorGroupId = 1, NotificationId = 1 };
            var monitorList = new MonitorGroup()
            {
                Name = "test",
                Monitors = new List<Monitor>
                {
                    new Monitor
                    {
                        Name = "test",
                        HeartBeatInterval = 1,
                        Retries = 1
                    }
                }
            };

            _monitorGroupServiceMock.Setup(repo => repo.GetMonitorGroupById(It.IsAny<int>()))
                .ReturnsAsync(monitorList);
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.GetMonitorNotifications(It.IsAny<int>()))
                .ReturnsAsync(new List<MonitorNotification>());
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.AddMonitorNotification(It.IsAny<MonitorNotification>()))
                .Returns(Task.CompletedTask);

            // Act
            await _monitorNotificationService.AddMonitorGroupNotification(monitorGroupNotification);

            // Assert
            _monitorNotificationRepositoryMock.Verify(
                repo => repo.AddMonitorNotification(It.IsAny<MonitorNotification>()), Times.Once);
        }

        [Fact]
        public async Task RemoveMonitorGroupNotification_RemovesGroupNotification()
        {
            // Arrange
            var monitorGroupNotification = new MonitorGroupNotification { MonitorGroupId = 1, NotificationId = 1 };

            var monitorList = new MonitorGroup()
            {
                Name = "test",
                Monitors = new List<Monitor>
                {
                    new Monitor
                    {
                        Name = "test",
                        HeartBeatInterval = 1,
                        Retries = 1
                    }
                }
            };

            _monitorGroupServiceMock.Setup(repo => repo.GetMonitorGroupById(It.IsAny<int>()))
                .ReturnsAsync(monitorList);
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.GetMonitorNotifications(It.IsAny<int>()))
                .ReturnsAsync(new List<MonitorNotification>());
            _monitorNotificationRepositoryMock
                .Setup(repo => repo.RemoveMonitorNotification(It.IsAny<MonitorNotification>()))
                .Returns(Task.CompletedTask);

            // Act
            await _monitorNotificationService.RemoveMonitorGroupNotification(monitorGroupNotification);

            // Assert
            _monitorNotificationRepositoryMock.Verify(
                repo => repo.RemoveMonitorNotification(It.IsAny<MonitorNotification>()), Times.Once);
        }
    }
}