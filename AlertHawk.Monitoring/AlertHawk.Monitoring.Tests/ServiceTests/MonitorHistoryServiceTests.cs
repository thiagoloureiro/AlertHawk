using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests
{
    public class MonitorHistoryServiceTests
    {
        private readonly Mock<IMonitorHistoryRepository> _monitorHistoryRepositoryMock;
        private readonly Mock<ICaching> _cachingMock;
        private readonly MonitorHistoryService _monitorHistoryService;

        public MonitorHistoryServiceTests()
        {
            _cachingMock = new Mock<ICaching>();
            _monitorHistoryRepositoryMock = new Mock<IMonitorHistoryRepository>();
            _monitorHistoryService = new MonitorHistoryService(_cachingMock.Object, _monitorHistoryRepositoryMock.Object);
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
            _monitorHistoryRepositoryMock
                .Setup(repo => repo.GetMonitorHistoryByIdAndDays(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(history);

            // Act
            var result = await _monitorHistoryService.GetMonitorHistory(1, 7);

            // Assert
            Assert.Equal(history, result);
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
        public async Task SetMonitorHistoryRetention_SetsRetention()
        {
            // Arrange
            var days = 7;

            // Act
            await _monitorHistoryService.SetMonitorHistoryRetention(days);

            // Assert
            _monitorHistoryRepositoryMock.Verify(repo => repo.SetMonitorHistoryRetention(days), Times.Once);
        }

        [Fact]
        public async Task GetMonitorHistoryRetention_ReturnsRetention()
        {
            // Arrange
            var retention = new MonitorSettings();
            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistoryRetention()).ReturnsAsync(retention);

            // Act
            var result = await _monitorHistoryService.GetMonitorHistoryRetention();

            // Assert
            Assert.Equal(retention, result);
        }

        [Fact]
        public async Task GetMonitorHistoryCount_ReturnsCount()
        {
            // Arrange
            var count = 10;
            _cachingMock.Setup(cache => cache.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<long>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()
            )).ReturnsAsync(count);

            // Act
            var result = await _monitorHistoryService.GetMonitorHistoryCount();

            // Assert
            Assert.Equal(count, result);
        }
    }
}