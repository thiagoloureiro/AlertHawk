using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using NSubstitute;
using System.Net;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.RunnerTests
{
    public class HttpClientRunnerTests : IClassFixture<HttpClientRunner>
    {
        private readonly IHttpClientRunner _httpClientRunner;
        private readonly IMonitorRepository _monitorRepository;
        private readonly INotificationProducer _notificationProducer;
        private readonly IMonitorAlertRepository _monitorAlertRepository;
        private readonly IMonitorHistoryRepository _monitorHistoryRepository;

        public HttpClientRunnerTests()
        {
            _monitorRepository = Substitute.For<IMonitorRepository>();
            _notificationProducer = Substitute.For<INotificationProducer>();
            _monitorAlertRepository = Substitute.For<IMonitorAlertRepository>();
            _monitorHistoryRepository = Substitute.For<IMonitorHistoryRepository>();

            _httpClientRunner = new HttpClientRunner(_monitorRepository, _notificationProducer, _monitorAlertRepository, _monitorHistoryRepository);
        }

        [Theory]
        [InlineData("https://postman-echo.com/get", MonitorHttpMethod.Get)]
        [InlineData("https://postman-echo.com/post", MonitorHttpMethod.Post)]
        [InlineData("https://postman-echo.com/put", MonitorHttpMethod.Put)]
        public async Task Should_Make_HttpClient_Call_OK_Result(string url, MonitorHttpMethod method)
        {
            // Arrange
            var monitorHttp = new MonitorHttp
            {
                UrlToCheck = url,
                MonitorId = 1,
                Name = "Test",
                Id = 1,
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10,
                MonitorHttpMethod = method,
                MaxRedirects = 5,
                HeartBeatInterval = 1,
                Retries = 0,
                LastStatus = true,
                ResponseTime = 10
            };

            // Act
            var result = await _httpClientRunner.MakeHttpClientCall(monitorHttp);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.ReasonPhrase);
            Assert.NotNull(result.Content);
        }

        [Theory]
        [InlineData("https://postman-echo.com/get", MonitorHttpMethod.Get)]
        [InlineData("https://postman-echo.com/post", MonitorHttpMethod.Post)]
        [InlineData("https://postman-echo.com/put", MonitorHttpMethod.Put)]
        public async Task Should_Make_HttpClient_Call_IgnoreTlsSsl_OK_Result(string url, MonitorHttpMethod method)
        {
            // Arrange
            var monitorHttp = new MonitorHttp
            {
                UrlToCheck = url,
                MonitorId = 1,
                Name = "Test",
                Id = 1,
                CheckCertExpiry = true,
                IgnoreTlsSsl = true,
                Timeout = 10,
                MonitorHttpMethod = method,
                MaxRedirects = 5,
                HeartBeatInterval = 1,
                Retries = 0,
                LastStatus = true,
                ResponseTime = 10
            };

            // Act
            var result = await _httpClientRunner.MakeHttpClientCall(monitorHttp);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.ReasonPhrase);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task CheckUrlsAsync_Should_Update_Monitor_Status_On_Success()
        {
            // Arrange
            var monitorHttp = new MonitorHttp
            {
                UrlToCheck = "https://postman-echo.com/get",
                MonitorId = 1,
                Name = "Test",
                Id = 1,
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10,
                MonitorHttpMethod = MonitorHttpMethod.Get,
                MaxRedirects = 5,
                HeartBeatInterval = 1,
                Retries = 1,
                LastStatus = false,
                ResponseTime = 10
            };

            var monitor = new Monitor
            {
                Id = 1,
                Status = false,
                Name = "Name",
                HeartBeatInterval = 0,
                Retries = 0
            };

            _monitorRepository.GetMonitorById(1).Returns(monitor);

            // Act
            await _httpClientRunner.CheckUrlsAsync(monitorHttp);

            // Assert
            await _monitorRepository.Received(1).UpdateMonitorStatus(1, true, Arg.Any<int>());
            await _monitorHistoryRepository.Received(1).SaveMonitorHistory(Arg.Any<MonitorHistory>());
            await _notificationProducer.Received(1).HandleSuccessNotifications(monitorHttp, "OK");
            await _monitorAlertRepository.Received(1).SaveMonitorAlert(Arg.Any<MonitorHistory>(), Arg.Any<MonitorEnvironment>());
        }

        [Fact]
        public async Task CheckUrlsAsync_Should_Update_Monitor_Status_On_Failure()
        {
            // Arrange
            var monitorHttp = new MonitorHttp
            {
                UrlToCheck = "https://postman-echo1.com/get",
                MonitorId = 1,
                Name = "Test",
                Id = 1,
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10,
                MonitorHttpMethod = MonitorHttpMethod.Get,
                MaxRedirects = 5,
                HeartBeatInterval = 1,
                Retries = 1,
                LastStatus = false,
                ResponseTime = 10
            };

            var monitor = new Monitor
            {
                Id = 1,
                Status = false,
                Name = "Name",
                HeartBeatInterval = 0,
                Retries = 0
            };

            _monitorRepository.GetMonitorById(1).Returns(monitor);

            // Act
            await _httpClientRunner.CheckUrlsAsync(monitorHttp);

            // Assert
            await _monitorRepository.Received(1).UpdateMonitorStatus(1, false, Arg.Any<int>());
        }

        [Fact]
        public async Task Should_Make_HttpClient_Call_Timeout()
        {
            // Arrange
            var monitorHttp = new MonitorHttp
            {
                UrlToCheck = "https://postman-echo.com/delay/10",
                MonitorId = 1,
                Name = "Test",
                Id = 1,
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 1,
                MonitorHttpMethod = MonitorHttpMethod.Get,
                MaxRedirects = 5,
                HeartBeatInterval = 1,
                Retries = 0,
            };

            // Act
            var result = await _httpClientRunner.MakeHttpClientCall(monitorHttp);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }
    }
}