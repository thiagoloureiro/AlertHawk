using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class HealthCheckServiceTests
{
    private readonly Mock<IHealthCheckRepository> _healthCheckRepositoryMock;
    private readonly Mock<ICaching> _cachingMock;
    private readonly IHealthCheckService _healthCheckService;

    public HealthCheckServiceTests()
    {
        _healthCheckRepositoryMock = new Mock<IHealthCheckRepository>();
        _cachingMock = new Mock<ICaching>();
        _healthCheckService = new HealthCheckService(_healthCheckRepositoryMock.Object, _cachingMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnFalse_WhenHealthCheckAndCacheOperationsSucceed()
    {
        // Arrange
        _healthCheckRepositoryMock.Setup(h => h.CheckHealthAsync()).ReturnsAsync(true);

        // Act
        var result = await _healthCheckService.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnTrue_WehnHealthCheckAndCacheOpeationsSucced()
    {
        // Arrange
        _cachingMock.Setup(c => c.SetValueToCacheAsync("CacheKey", "CacheValue", 120, CacheTimeInterval.Minutes)).Returns(Task.CompletedTask);
        _cachingMock.Setup(c => c.GetValueFromCacheAsync<string>("CacheKey")).ReturnsAsync("CacheValue");
        _healthCheckRepositoryMock.Setup(h => h.CheckHealthAsync()).ReturnsAsync(true);

        // Act
        var result = await _healthCheckService.CheckHealthAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCaptureExceptionAndReturnFalse_WhenHealthCheckRepositoryThrowsException()
    {
        // Arrange
        _cachingMock.Setup(c => c.SetValueToCacheAsync("CacheKey", "CacheValue", 120, CacheTimeInterval.Minutes)).Returns(Task.CompletedTask);
        _cachingMock.Setup(c => c.GetValueFromCacheAsync<string>("CacheKey")).ReturnsAsync("CacheValue");
        _healthCheckRepositoryMock.Setup(h => h.CheckHealthAsync()).ThrowsAsync(new Exception("Repository error"));

        // Act
        var result = await _healthCheckService.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }
}