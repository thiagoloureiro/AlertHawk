using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class TcpClientRunnerTests : IClassFixture<HttpClientRunner>
{
    private readonly ITcpClientRunner _tcpClientRunner;
    private readonly ITcpClientRunner _tcpClientRunner2;
    private readonly Mock<ITcpClientRunner> _tcpClientRunnerMock;
    private readonly Mock<IMonitorRepository> _mockMonitorRepository;
    private readonly Mock<INotificationProducer> _mockNotificationProducer;
    private readonly Mock<IMonitorHistoryRepository> _mockMonitorHistoryRepository;
    private readonly Mock<ISystemConfigurationRepository> _mockSystemConfigurationRepository;
    private readonly Mock<ILogger<TcpClientRunner>> _logger;

    public TcpClientRunnerTests(ITcpClientRunner tcpClientRunner)
    {
        _tcpClientRunner = tcpClientRunner;
        _mockMonitorRepository = new Mock<IMonitorRepository>();
        _mockNotificationProducer = new Mock<INotificationProducer>();
        _mockMonitorHistoryRepository = new Mock<IMonitorHistoryRepository>();
        _mockSystemConfigurationRepository = new Mock<ISystemConfigurationRepository>();
        _tcpClientRunnerMock = new Mock<ITcpClientRunner>();
        _logger = new Mock<ILogger<TcpClientRunner>>();
        
        // Setup default behavior: monitors are enabled
        _mockSystemConfigurationRepository.Setup(x => x.IsMonitorExecutionDisabled()).ReturnsAsync(false);
        
        _tcpClientRunner2 = new TcpClientRunner(_mockMonitorRepository.Object, _mockNotificationProducer.Object, _mockMonitorHistoryRepository.Object, _mockSystemConfigurationRepository.Object, _logger.Object);
    }

    [Theory]
    [InlineData("8.8.8.8", 443)]
    [InlineData("1.1.1.1", 443)]
    public async Task Should_Make_Tcp_Call_Success_Result(string ip, int port)
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            Id = 1,
            Name = "Test",
            Timeout = 10,
            Port = port,
            IP = ip,
            HeartBeatInterval = 1,
            Retries = 0,
            LastStatus = false,
        };

        // Act
        var result = await _tcpClientRunner.MakeTcpCall(monitorTcp);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("127.0.0.1", 4434)]
    public async Task Should_Make_Tcp_Call_Failed_Result(string ip, int port)
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            Id = 1,
            Name = "Test",
            Timeout = 20,
            Port = port,
            IP = ip,
            HeartBeatInterval = 1,
            Retries = 0,
        };

        // Act
        var result = await _tcpClientRunner.MakeTcpCall(monitorTcp);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("127.0.0.1", 4434131)]
    public async Task Should_Make_Tcp_Call_InvalidPort_Failed_Result(string ip, int port)
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            Id = 1,
            Name = "Test",
            Timeout = 20,
            Port = port,
            IP = ip,
            HeartBeatInterval = 1,
            Retries = 0,
        };

        // Act
        var result = await _tcpClientRunner.MakeTcpCall(monitorTcp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckTcpAsync_ShouldReturnTrue_WhenConnectionIsSuccessful()
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            MonitorId = 1,
            IP = "1.1.1.1",
            Port = 443,
            Retries = 3,
            Timeout = 5,
            LastStatus = false,
            CheckCertExpiry = false,
            Name = "Name",
            HeartBeatInterval = 1,
        };

        _tcpClientRunnerMock.Setup(r => r.MakeTcpCall(It.IsAny<MonitorTcp>())).ReturnsAsync(true);

        // Act
        var result = await _tcpClientRunner2.CheckTcpAsync(monitorTcp);

        // Assert
        Assert.True(result);
        _mockMonitorHistoryRepository.Verify(m => m.SaveMonitorHistory(It.IsAny<MonitorHistory>()), Times.Once);
        _mockMonitorRepository.Verify(m => m.UpdateMonitorStatus(monitorTcp.MonitorId, true, 0), Times.Once);
        _mockNotificationProducer.Verify(n => n.HandleSuccessTcpNotifications(monitorTcp), Times.Once);
    }

    [Fact]
    public async Task CheckTcpAsync_ShouldReturnTrue_WhenConnectionFails()
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            MonitorId = 1,
            IP = "127.1.1.1",
            Port = 123123,
            Retries = 3,
            Timeout = 5,
            LastStatus = false,
            CheckCertExpiry = false,
            Name = "Name",
            HeartBeatInterval = 1,
        };

        _tcpClientRunnerMock.Setup(r => r.MakeTcpCall(It.IsAny<MonitorTcp>())).ReturnsAsync(true);

        // Act
        var result = await _tcpClientRunner2.CheckTcpAsync(monitorTcp);

        // Assert
        Assert.False(result);
    }
}