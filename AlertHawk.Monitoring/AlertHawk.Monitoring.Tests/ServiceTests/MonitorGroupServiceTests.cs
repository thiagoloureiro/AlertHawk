using System.Net;
using System.Text;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using EasyMemoryCache.Extensions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class MonitorGroupServiceTests
{
    private readonly Mock<IMonitorGroupRepository> _monitorGroupRepositoryMock;
    private readonly Mock<ICaching> _cachingMock;
    private readonly Mock<IMonitorRepository> _monitorRepositoryMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly MonitorGroupService _monitorGroupService;

    public MonitorGroupServiceTests()
    {
        _monitorGroupRepositoryMock = new Mock<IMonitorGroupRepository>();
        _cachingMock = new Mock<ICaching>();
        _monitorRepositoryMock = new Mock<IMonitorRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _monitorGroupService = new MonitorGroupService(
            _monitorGroupRepositoryMock.Object,
            _cachingMock.Object,
            _monitorRepositoryMock.Object,
            _httpClientFactoryMock.Object
        );
    }

    [Fact]
    public async Task GetMonitorGroupList_ShouldReturnMonitorGroups_FromCache()
    {
        // Arrange
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup { Id = 1, Name = "Group1" },
            new MonitorGroup { Id = 2, Name = "Group2" }
        };

        _cachingMock.Setup(caching => caching.GetOrSetObjectFromCacheAsync(
                "MonitorGroupList", 10, It.IsAny<Func<Task<IEnumerable<MonitorGroup>>>>(), It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .ReturnsAsync(monitorGroups);

        // Act
        var result = await _monitorGroupService.GetMonitorGroupList();

        // Assert
        Assert.Equal(monitorGroups, result);
    }

    [Fact]
    public async Task GetMonitorGroupList_ShouldReturnMonitorGroups_FromRepository()
    {
        // Arrange
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup { Id = 1, Name = "Group1" },
            new MonitorGroup { Id = 2, Name = "Group2" }
        };

        // Simulate the caching mechanism to call the repository method
        _cachingMock.Setup(caching => caching.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<Func<Task<IEnumerable<MonitorGroup>>>>(), 
                It.IsAny<bool>(), 
                It.IsAny<CacheTimeInterval>()))
            .Returns((string key, int duration, Func<Task<IEnumerable<MonitorGroup>>> fetchFunction, bool arg4, CacheTimeInterval arg5) => fetchFunction());

        _monitorGroupRepositoryMock.Setup(repo => repo.GetMonitorGroupList())
            .ReturnsAsync(monitorGroups);

        var monitorGroupService = new MonitorGroupService(
            _monitorGroupRepositoryMock.Object,
            _cachingMock.Object,
            _monitorRepositoryMock.Object,
            _httpClientFactoryMock.Object
        );

        // Act
        var result = await monitorGroupService.GetMonitorGroupList();

        // Assert
        Assert.Equal(monitorGroups, result);
        _monitorGroupRepositoryMock.Verify(repo => repo.GetMonitorGroupList(), Times.Once);
        _cachingMock.Verify(caching => caching.GetOrSetObjectFromCacheAsync(
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<Func<Task<IEnumerable<MonitorGroup>>>>(), 
            It.IsAny<bool>(), 
            It.IsAny<CacheTimeInterval>()), Times.Once);
    }

    [Fact]
    public async Task GetMonitorGroupListByEnvironment_ShouldReturnFilteredMonitorGroups()
    {
        // Arrange
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1, Name = "Group1", Monitors = new List<Monitor>
                {
                    new Monitor() { Id = 1, Name =  null, HeartBeatInterval = 0, Retries = 0}
                }
            },
            new MonitorGroup
            {
                Id = 2, Name = "Group2", Monitors = new List<Monitor>
                {
                    new Monitor() { Id = 2, Name =  null, HeartBeatInterval = 0, Retries = 0}
                }
            }
        };

        var groupIds = new List<int> { 1 };

        _monitorGroupRepositoryMock.Setup(repo => repo.GetMonitorGroupListByEnvironment(It.IsAny<MonitorEnvironment>()))
            .ReturnsAsync(monitorGroups);

        _monitorGroupRepositoryMock.Setup(repo => repo.GetMonitorGroupList())
            .ReturnsAsync(monitorGroups);

        _cachingMock.Setup(caching => caching.GetValueFromCacheAsync<List<MonitorDashboard?>>(
                It.IsAny<string>()))
            .ReturnsAsync(new List<MonitorDashboard?>());
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new List<UsersMonitorGroup>
                {
                    new UsersMonitorGroup { GroupMonitorId = 1 }
                }), Encoding.UTF8, "application/json")
            });

        // Act
        var result =
            await _monitorGroupService.GetMonitorGroupListByEnvironment("jwtToken", MonitorEnvironment.Production);

        // Assert
        Assert.Single(result);
        Assert.Equal("Group1", result.First().Name);
    }

    [Fact]
    public async Task GetMonitorGroupList_ShouldReturnNull_WhenNoGroupIdsAreAvailable()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _monitorGroupRepositoryMock.Setup(repo => repo.GetMonitorGroupList())
            .ReturnsAsync(new List<MonitorGroup>());

        // Act
        var result = await _monitorGroupService.GetMonitorGroupList("jwtToken");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddMonitorToGroup_ShouldInvalidateCache()
    {
        // Arrange
        var monitorGroupItems = new MonitorGroupItems { MonitorId = 1, MonitorGroupId = 1 };

        _monitorGroupRepositoryMock.Setup(repo => repo.AddMonitorToGroup(It.IsAny<MonitorGroupItems>()))
            .Returns(Task.CompletedTask);

        // Act
        await _monitorGroupService.AddMonitorToGroup(monitorGroupItems);

        // Assert
        _cachingMock.Verify(caching => caching.Invalidate(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMonitorGroup_ShouldInvalidateCache()
    {
        // Arrange
        _monitorGroupRepositoryMock.Setup(repo => repo.DeleteMonitorGroup(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        await _monitorGroupService.DeleteMonitorGroup("jwtToken", 1);

        // Assert
        _cachingMock.Verify(caching => caching.Invalidate(It.IsAny<string>()), Times.Once);
    }
}