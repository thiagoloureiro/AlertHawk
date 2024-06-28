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
    public class MonitorServiceTests
    {
        private readonly Mock<IMonitorRepository> _monitorRepositoryMock;
        private readonly Mock<ICaching> _cachingMock;
        private readonly Mock<IMonitorGroupService> _monitorGroupServiceMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IHttpClientRunner> _httpClientRunnerMock;
        private readonly MonitorService _monitorService;
        private readonly MonitorHistoryService _monitorHistoryService;
        private readonly MonitorNotificationService _monitorNotificationService;
        private readonly Mock<IMonitorNotificationRepository> _monitorNotificationRepositoryMock;
        private readonly Mock<IMonitorNotificationService> _monitorNotificationServiceMock;
        private readonly Mock<IMonitorHistoryService> _monitorHistoryServiceMock;
        private readonly Mock<IMonitorHistoryRepository> _monitorHistoryRepositoryMock;

        public MonitorServiceTests(Mock<IMonitorNotificationRepository> monitorNotificationRepositoryMock, Mock<IMonitorNotificationService> monitorNotificationServiceMock, Mock<IMonitorHistoryService> monitorHistoryServiceMock, Mock<IMonitorHistoryRepository> monitorHistoryRepositoryMock, MonitorHistoryService monitorHistoryService)
        {
            _monitorNotificationRepositoryMock = monitorNotificationRepositoryMock;
            _monitorNotificationServiceMock = monitorNotificationServiceMock;
            _monitorHistoryServiceMock = monitorHistoryServiceMock;
            _monitorHistoryRepositoryMock = monitorHistoryRepositoryMock;
            _monitorHistoryService = new MonitorHistoryService(_cachingMock.Object, _monitorHistoryRepositoryMock.Object);
           _monitorNotificationService = new MonitorNotificationService(_monitorGroupServiceMock.Object, _monitorNotificationRepositoryMock.Object);
            _monitorRepositoryMock = new Mock<IMonitorRepository>();
            _cachingMock = new Mock<ICaching>();
            _monitorGroupServiceMock = new Mock<IMonitorGroupService>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientRunnerMock = new Mock<IHttpClientRunner>();
            _monitorService = new MonitorService(_monitorRepositoryMock.Object, _cachingMock.Object,
                _monitorGroupServiceMock.Object, _httpClientFactoryMock.Object, _httpClientRunnerMock.Object, _monitorHistoryRepositoryMock.Object);
        }

        [Fact]
        public async Task GetMonitorNotifications_ReturnsNotifications()
        {
            // Arrange
            var notifications = new List<MonitorNotification> { new MonitorNotification() };
            _monitorNotificationRepositoryMock.Setup(repo => repo.GetMonitorNotifications(It.IsAny<int>())).ReturnsAsync(notifications);

            // Act
            var result = await _monitorNotificationService.GetMonitorNotifications(1);

            // Assert
            Assert.Equal(notifications, result);
        }

        [Fact]
        public async Task GetMonitorHistory_ReturnsHistory()
        {
            // Arrange
            var history = new List<MonitorHistory> { new MonitorHistory() };
            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistory(It.IsAny<int>())).ReturnsAsync(history);

            // Act
            var result = await _monitorHistoryService.GetMonitorHistory(1);

            // Assert
            Assert.Equal(history, result);
        }

        [Fact]
        public async Task GetMonitorHistoryByDays_ReturnsHistory()
        {
            // Arrange
            var history = new List<MonitorHistory> { new MonitorHistory() };
            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistoryByIdAndDays(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(history);

            // Act
            var result = await _monitorHistoryService.GetMonitorHistory(1, 7);

            // Assert
            Assert.Equal(history, result);
        }

        [Fact]
        public async Task PauseMonitor_UpdatesRepositoryAndInvalidatesCache()
        {
            // Arrange
            var monitorId = 1;
            var paused = true;

            // Act
            await _monitorService.PauseMonitor(monitorId, paused);

            // Assert
            _monitorRepositoryMock.Verify(repo => repo.PauseMonitor(monitorId, paused), Times.Once);
            _cachingMock.Verify(cache => cache.Invalidate($"Monitor_{monitorId}"), Times.Once);
        }

        [Fact]
        public async Task GetMonitorDashboardData_ReturnsMonitorDashboard()
        {
            // Arrange
            var monitorId = 1;
            var monitorHistory = new List<MonitorHistory>
            {
                new MonitorHistory { TimeStamp = DateTime.UtcNow.AddDays(-1), Status = true, ResponseTime = 100 },
                new MonitorHistory { TimeStamp = DateTime.UtcNow.AddDays(-2), Status = false, ResponseTime = 200 },
            };
            var monitor = new Monitor()
                { Id = monitorId, DaysToExpireCert = 30, Name = "Name", HeartBeatInterval = 1, Retries = 0 };

            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistoryByIdAndDays(monitorId, 90))
                .ReturnsAsync(monitorHistory);
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorById(monitorId)).ReturnsAsync(monitor);
            _cachingMock.Setup(caching => caching.GetOrSetObjectFromCacheAsync(
                    $"Monitor_{monitorId}", 
                    It.IsAny<int>(), 
                    It.IsAny<Func<Task<Monitor>>>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<CacheTimeInterval>()))
                .ReturnsAsync(monitor);

            // Act
            var result = await _monitorService.GetMonitorDashboardData(monitorId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(monitorId, result.MonitorId);
            Assert.Equal(150, result.ResponseTime); // (100 + 200) / 2
        }

        [Fact]
        public async Task GetMonitorDashboardData_NoHistory_ReturnsEmptyDashboard()
        {
            // Arrange
            var monitorId = 1;
            var monitorHistory = new List<MonitorHistory>();
            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistoryByIdAndDays(monitorId, 90))
                .ReturnsAsync(monitorHistory);

            // Act
            var result = await _monitorService.GetMonitorDashboardData(monitorId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(monitorId, result.MonitorId);
            Assert.Equal(0, result.ResponseTime);
        }

        [Fact]
        public async Task GetMonitorList_ReturnsMonitorList()
        {
            // Arrange
            var monitorList = new List<Monitor> { new Monitor
                {
                    Name = null,
                    HeartBeatInterval = 0,
                    Retries = 0
                }
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorList()).ReturnsAsync(monitorList);

            // Act
            var result = await _monitorService.GetMonitorList();

            // Assert
            Assert.Equal(monitorList, result);
        }

        [Fact]
        public async Task DeleteMonitorHistory_DeletesHistory()
        {
            // Arrange
            var days = 7;

            // Act
            await _monitorHistoryService.DeleteMonitorHistory(days);

            // Assert
            _monitorHistoryRepositoryMock.Verify(repo => repo.DeleteMonitorHistory(days), Times.Once);
        }

        [Fact]
        public async Task CreateMonitorHttp_SetsHeadersJsonAndCallsRunner()
        {
            // Arrange
            var monitor = new MonitorHttp()
            {
                Id = 1, DaysToExpireCert = 30, Name = "Name", HeartBeatInterval = 1, Retries = 0, MaxRedirects = 1,
                UrlToCheck = "https://www.abb.com/", Timeout = 0,
                Headers = new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("key", "value")
                }
            };

            _monitorRepositoryMock.Setup(repo => repo.CreateMonitorHttp(It.IsAny<MonitorHttp>())).ReturnsAsync(1);

            // Act
            var result = await _monitorService.CreateMonitorHttp(monitor);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(monitor.HeadersJson);
            _httpClientRunnerMock.Verify(runner => runner.CheckUrlsAsync(monitor), Times.Once);
        }

        [Fact]
        public async Task UpdateMonitorHttp_SetsHeadersJsonAndUpdatesRepository()
        {
            // Arrange
            var monitor = new MonitorHttp()
            {
                Id = 1, DaysToExpireCert = 30, Name = "Name", HeartBeatInterval = 1, Retries = 0, MaxRedirects = 1,
                UrlToCheck = "https://www.abb.com/", Timeout = 0,
                Headers = new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("key", "value")
                }
            };

            // Act
            await _monitorService.UpdateMonitorHttp(monitor);

            // Assert
            Assert.NotNull(monitor.HeadersJson);
            _monitorRepositoryMock.Verify(repo => repo.UpdateMonitorHttp(monitor), Times.Once);
        }

        [Fact]
        public async Task DeleteMonitor_DeletesMonitorAndLogsAction()
        {
            // Arrange
            var monitorId = 1;
            var jwtToken = "testToken";
            var monitor = new Monitor
            {
                Id = monitorId,
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorById(monitorId)).ReturnsAsync(monitor);
            _monitorRepositoryMock.Setup(repo => repo.DeleteMonitor(monitorId)).Returns(Task.CompletedTask);

            // Act
            await _monitorService.DeleteMonitor(monitorId, jwtToken);

            // Assert
            _monitorRepositoryMock.Verify(repo => repo.DeleteMonitor(monitorId), Times.Once);
            
        }

      

        [Fact]
        public async Task CreateMonitor_SetsHeadersJsonAndCallsRepository()
        {
            // Arrange
            var monitor = new MonitorHttp
            {
                Name = "Test Monitor",
                Headers = new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("key",
                        "value")
                },
                MaxRedirects = 0,
                UrlToCheck = "http://www.google.com",
                Timeout = 0,
                HeartBeatInterval = 0,
                Retries = 0
            };
            _monitorRepositoryMock.Setup(repo => repo.CreateMonitorHttp(monitor)).ReturnsAsync(1);

            // Act
            var result = await _monitorService.CreateMonitorHttp(monitor);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(monitor.HeadersJson);
        }
    }
}
