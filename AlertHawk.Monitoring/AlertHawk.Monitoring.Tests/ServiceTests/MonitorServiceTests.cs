using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.ServiceTests
{
    public class MonitorServiceTests
    {
        private readonly Mock<IMonitorRepository> _monitorRepositoryMock;
        private readonly Mock<IMonitorHistoryRepository> _monitorHistoryRepositoryMock;
        private readonly Mock<ICaching> _cachingMock;
        private readonly Mock<IHttpClientRunner> _httpClientRunnerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly MonitorService _monitorService;
        private readonly Mock<IMonitorGroupService> _monitorGroupServiceMock;
        private readonly string _cacheKeyDashboardList = "MonitorDashboardList";
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<ILogger<MonitorService>> _loggerMock;

        public MonitorServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _monitorRepositoryMock = new Mock<IMonitorRepository>();
            _monitorHistoryRepositoryMock = new Mock<IMonitorHistoryRepository>();
            _monitorGroupServiceMock = new Mock<IMonitorGroupService>();
            _cachingMock = new Mock<ICaching>();
            _httpClientRunnerMock = new Mock<IHttpClientRunner>();
            _loggerMock = new Mock<ILogger<MonitorService>>();
            _monitorService = new MonitorService(_monitorRepositoryMock.Object, _cachingMock.Object,
                _monitorGroupServiceMock.Object, _httpClientFactoryMock.Object, _httpClientRunnerMock.Object,
                _monitorHistoryRepositoryMock.Object, _loggerMock.Object);
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
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
                new MonitorHistory { TimeStamp = DateTime.UtcNow.AddMinutes(-10), Status = true, ResponseTime = 100 },
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
            var result = await _monitorService.GetMonitorDashboardData(monitorId, monitor);

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
            var result = await _monitorService.GetMonitorDashboardData(monitorId, It.IsAny<Monitor>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(monitorId, result.MonitorId);
            Assert.Equal(0, result.ResponseTime);
        }

        [Fact]
        public async Task GetMonitorList_ReturnsMonitorList()
        {
            // Arrange
            var monitorList = new List<Monitor>
            {
                new Monitor
                {
                    Name = "Name",
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
        public async Task CreateMonitorHttp_SetsHeadersJsonAndCallsRunner()
        {
            // Arrange
            var monitor = new MonitorHttp()
            {
                Id = 1,
                DaysToExpireCert = 30,
                Name = "Name",
                HeartBeatInterval = 1,
                Retries = 0,
                MaxRedirects = 1,
                UrlToCheck = "https://www.abb.com/",
                Timeout = 0,
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
        }

        [Fact]
        public async Task UpdateMonitorHttp_SetsHeadersJsonAndUpdatesRepository()
        {
            // Arrange
            var monitor = new MonitorHttp()
            {
                Id = 1,
                DaysToExpireCert = 30,
                Name = "Name",
                HeartBeatInterval = 1,
                Retries = 0,
                MaxRedirects = 1,
                UrlToCheck = "https://www.abb.com/",
                Timeout = 0,
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

        [Fact]
        public async Task CreateMonitorTcp_SetsHeadersJsonAndCallsRepository()
        {
            // Arrange
            var monitor = new MonitorTcp
            {
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0,
                IP = "1.1.1.1",
                Port = 80,
                Timeout = 1000
            };
            _monitorRepositoryMock.Setup(repo => repo.CreateMonitorTcp(monitor)).ReturnsAsync(1);

            // Act
            var result = await _monitorService.CreateMonitorTcp(monitor);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task UpdateMonitorTcp_SetsHeadersJsonAndCallsRepository()
        {
            // Arrange
            var monitor = new MonitorTcp
            {
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0,
                IP = "1.1.1.1",
                Port = 80,
                Timeout = 1000
            };
            _monitorRepositoryMock.Setup(repo => repo.UpdateMonitorTcp(monitor));

            // Act
            await _monitorService.UpdateMonitorTcp(monitor);

            // Assert
            _monitorRepositoryMock.Verify(repo => repo.UpdateMonitorTcp(monitor), Times.Once);
        }

        [Fact]
        public async Task GetHttpMonitorByMonitorId_ReturnsMonitor()
        {
            // Arrange
            var monitorId = 1;
            var monitor = new MonitorHttp
            {
                Id = monitorId,
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
            _monitorRepositoryMock.Setup(repo => repo.GetHttpMonitorByMonitorId(monitorId)).ReturnsAsync(monitor);

            // Act
            var result = await _monitorService.GetHttpMonitorByMonitorId(monitorId);

            // Assert
            Assert.Equal(monitor, result);
        }

        [Fact]
        public async Task GetTcpMonitorByMonitorId()
        {
            // Arrange
            var monitorId = 1;
            var monitor = new MonitorTcp
            {
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0,
                IP = "1.1.1.1",
                Port = 80,
                Timeout = 1000
            };

            _monitorRepositoryMock.Setup(repo => repo.GetTcpMonitorByMonitorId(monitorId)).ReturnsAsync(monitor);

            // Act
            var result = await _monitorService.GetTcpMonitorByMonitorId(monitorId);

            // Assert
            Assert.Equal(monitor, result);
        }

        [Fact]
        public async Task GetMonitorTagList_ReturnsMonitorTagList()
        {
            // Arrange
            var monitorTagList = new List<string>
            {
                "tag1",
                "tag2"
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorTagList()).ReturnsAsync(monitorTagList);

            // Act
            var result = await _monitorService.GetMonitorTagList();

            // Assert
            Assert.Equal(monitorTagList, result);
        }

        [Fact]
        public async Task GetMonitorListByTag_ReturnsMonitorList()
        {
            // Arrange
            var monitorList = new List<Monitor>
            {
                new Monitor
                {
                    Name = "",
                    HeartBeatInterval = 0,
                    Retries = 0
                }
            };
            var tag = "tag";
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorListbyTag(tag)).ReturnsAsync(monitorList);

            // Act
            var result = await _monitorService.GetMonitorListByTag(tag);

            // Assert
            Assert.Equal(monitorList, result);
        }

        [Fact]
        public async Task GetMonitorFailureCount_ReturnsMonitorFailureCount()
        {
            // Arrange
            var monitorId = 1;
            var monitorFailureCountList = new List<MonitorFailureCount>
            {
                new MonitorFailureCount
                {
                    MonitorId = monitorId,
                    FailureCount = 1
                }
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorFailureCount(monitorId))
                .ReturnsAsync(monitorFailureCountList);

            // Act
            var result = await _monitorService.GetMonitorFailureCount(monitorId);

            // Assert
            Assert.Equal(monitorFailureCountList, result);
        }

        [Fact]
        public async Task GetMonitorBackupJson_ReturnsMonitorBackupJson()
        {
            // Arrange
            var monitorId = 1;
            var monitor = new Monitor
            {
                Id = monitorId,
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorById(monitorId)).ReturnsAsync(monitor);

            // Act
            var result = await _monitorService.GetMonitorBackupJson();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetMonitorDashboardDataList_ReturnsMonitorDashboardDataList()
        {
            // Arrange
            var monitorId = 1;
            var monitorIds = new List<int> { 1 };
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
            var result = _monitorService.GetMonitorDashboardDataList(monitorIds);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PauseMonitorByGroupId_UpdatesRepositoryAndInvalidatesCache()
        {
            // Arrange
            var groupId = 1;
            var paused = true;
            var monitorGroup = new MonitorGroup
            {
                Name = "Name",
                Id = groupId,
                Monitors = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0,
                        Paused = false
                    }
                }
            };

            _monitorGroupServiceMock.Setup(x => x.GetMonitorGroupById(groupId)).ReturnsAsync(monitorGroup);

            // Act
            await _monitorService.PauseMonitorByGroupId(groupId, paused);

            // Assert
            _monitorRepositoryMock.Verify(repo => repo.PauseMonitor(groupId, paused), Times.Once);
        }

        [Fact]
        public async Task GetMonitorListByMonitorGroupIds_ReturnsMonitorList()
        {
            // Arrange
            string _cacheKeyDashboardList = "MonitorDashboardList";
            var monitorGroupIds = new List<int> { 1 };
            var token = "token";
            var lstDashboardData = new List<MonitorDashboard?>
            {
                new MonitorDashboard
                {
                    CertExpDays = 10,
                    MonitorId = 1,
                    ResponseTime = 10,
                    Uptime1Hr = 100,
                    Uptime3Months = 100,
                    Uptime6Months = 100,
                    Uptime24Hrs = 100,
                    Uptime7Days = 100,
                    Uptime30Days = 100,
                    HistoryData = new List<MonitorHistory>
                    {
                        new MonitorHistory
                        {
                            HttpVersion = "2.0",
                            MonitorId = 1,
                            ResponseTime = 10,
                            Status = true,
                            TimeStamp = DateTime.UtcNow,
                            Id = 1,
                            ResponseMessage = "Response",
                            StatusCode = 200
                        }
                    }
                }
            };

            var monitorList = new List<Monitor>
            {
                new Monitor
                {
                    Name = "Name",
                    HeartBeatInterval = 0,
                    Retries = 0,
                    Paused = false,
                    Id = 1
                }
            };

            _monitorGroupServiceMock.Setup(x => x.GetUserGroupMonitorListIds(token)).ReturnsAsync(monitorGroupIds);
            _monitorRepositoryMock
                .Setup(x => x.GetMonitorListByMonitorGroupIds(monitorGroupIds, MonitorEnvironment.All))
                .ReturnsAsync(monitorList);
            _cachingMock.Setup(caching => caching.GetValueFromCacheAsync<List<MonitorDashboard?>>(
                    _cacheKeyDashboardList))
                .ReturnsAsync(lstDashboardData);

            // Act
            var result = await _monitorService.GetMonitorListByMonitorGroupIds(token, MonitorEnvironment.All);

            // Assert
            Assert.Equal(monitorList, result);
        }

        [Fact]
        public async Task SetMonitorDashboardDataCacheList()
        {
            // Arrange
            string _cacheKeyDashboardList = "MonitorDashboardList";
            GlobalVariables.MasterNode = true;

            var monitorId = 1;
            var monitorHistory = new List<MonitorHistory>
            {
                new MonitorHistory { TimeStamp = new DateTime(), Status = true, ResponseTime = 100 },
                new MonitorHistory { TimeStamp = new DateTime(), Status = false, ResponseTime = 200 },
            };

            var monitorList = new List<Monitor>
            {
                new Monitor
                {
                    Name = "Name",
                    HeartBeatInterval = 0,
                    Retries = 0,
                    Id = 1
                }
            };

            var lstDashboardData = new List<MonitorDashboard?>
            {
                new MonitorDashboard
                {
                    CertExpDays = 10,
                    MonitorId = 1,
                    ResponseTime = 10,
                    Uptime1Hr = 100,
                    Uptime3Months = 100,
                    Uptime6Months = 100,
                    Uptime24Hrs = 100,
                    Uptime7Days = 100,
                    Uptime30Days = 100,
                    HistoryData = new List<MonitorHistory>
                    {
                        new MonitorHistory
                        {
                            HttpVersion = "2.0",
                            MonitorId = 1,
                            ResponseTime = 10,
                            Status = true,
                            Id = 1,
                            ResponseMessage = "Response",
                            StatusCode = 200,
                            TimeStamp = new DateTime()
                        }
                    }
                }
            };

            _monitorHistoryRepositoryMock.Setup(repo => repo.GetMonitorHistoryByIdAndDays(monitorId, 90))
                .ReturnsAsync(monitorHistory);
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorList()).ReturnsAsync(monitorList);

            _cachingMock.Setup(x => x.SetValueToCacheAsync(_cacheKeyDashboardList, lstDashboardData, It.IsAny<int>(),
                It.IsAny<CacheTimeInterval>()));

            // Act
            await _monitorService.SetMonitorDashboardDataCacheList();
        }

        [Fact]
        public async Task GetMonitorStatusDashboardData_ReturnsMonitorStatusDashboardData()
        {
            // Arrange
            string _cacheKeyDashboardList = "MonitorDashboardList";
            GlobalVariables.MasterNode = true;
            string token = "token";
            var monitorGroupIds = new List<int> { 1 };
            var monitor = new Monitor()
            { Id = 1, DaysToExpireCert = 30, Name = "Name", HeartBeatInterval = 1, Retries = 0 };

            var monitorHistory = new List<MonitorHistory>
            {
                new MonitorHistory { TimeStamp = DateTime.UtcNow.AddDays(-1), Status = true, ResponseTime = 100 },
                new MonitorHistory { TimeStamp = DateTime.UtcNow.AddDays(-2), Status = false, ResponseTime = 200 },
            };
            var monitorId = 1;
            _monitorGroupServiceMock.Setup(x => x.GetUserGroupMonitorListIds(token)).ReturnsAsync(monitorGroupIds);
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
            var result = await _monitorService.GetMonitorDashboardData(monitorId, monitor);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(monitorId, result.MonitorId);
            Assert.Equal(150, result.ResponseTime); // (100 + 200) / 2
        }

        [Fact]
        public async Task GetUserDetailsByToken_ReturnsUserDetails()
        {
            // Arrange
            var authApi = "https://fakeUrl/auth/";
            Environment.SetEnvironmentVariable("AUTH_API_URL", authApi);
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

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
                    Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _monitorService.GetUserDetailsByToken("jwtToken");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetMonitorById_ReturnsMonitor()
        {
            // Arrange
            var monitorId = 1;
            var monitor = new Monitor
            {
                Id = monitorId,
                Name = "Test Monitor",
                HeartBeatInterval = 0,
                Retries = 0
            };
            _monitorRepositoryMock.Setup(repo => repo.GetMonitorById(monitorId)).ReturnsAsync(monitor);

            // Act
            var result = await _monitorService.GetMonitorById(monitorId);

            // Assert
            Assert.Equal(monitor, result);
        }

        [Fact]
        public async Task UploadMonitorJsonBackup_RestoresMonitors()
        {
            // Arrange
            var monitorBackupJson = new MonitorBackup
            {
                MonitorGroupList = new List<MonitorGroup>
                {
                    new MonitorGroup
                    {
                        Name = "Group1",
                        Id = 1
                    }
                },
            };

            _monitorRepositoryMock.Setup(repo => repo.WipeMonitorData()).Returns(Task.CompletedTask);
            _monitorRepositoryMock.Setup(repo => repo.CreateMonitorHttp(It.IsAny<MonitorHttp>())).ReturnsAsync(1);

            // Act
            await _monitorService.UploadMonitorJsonBackup(monitorBackupJson);

            // Assert
            _monitorRepositoryMock.Verify(repo => repo.WipeMonitorData(), Times.Once);
        }
    }
}