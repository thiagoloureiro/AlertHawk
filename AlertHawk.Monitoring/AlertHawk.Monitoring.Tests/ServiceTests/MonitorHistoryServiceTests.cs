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
    }
}