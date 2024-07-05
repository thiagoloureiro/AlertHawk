using System.Net.Sockets;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class TcpClientRunnerTests : IClassFixture<HttpClientRunner>
{
    private readonly ITcpClientRunner _tcpClientRunner;

    public TcpClientRunnerTests(ITcpClientRunner tcpClientRunner)
    {
        _tcpClientRunner = tcpClientRunner;
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
}