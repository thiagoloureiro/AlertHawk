using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Moq;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.ServiceTests
{
    public class MonitorNotificationServiceTests
    {
        private readonly Mock<IMonitorNotificationRepository> _monitorNotificationRepositoryMock;
        private readonly Mock<IMonitorGroupService> _monitorGroupServiceMock;
        private readonly IMonitorNotificationService _monitorNotificationService;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        
        public MonitorNotificationServiceTests()
        {
            _monitorNotificationRepositoryMock = new Mock<IMonitorNotificationRepository>();
            _monitorGroupServiceMock = new Mock<IMonitorGroupService>();
            _monitorNotificationService = new MonitorNotificationService(_monitorGroupServiceMock.Object, _monitorNotificationRepositoryMock.Object);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
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
    }
}