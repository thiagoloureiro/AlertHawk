using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class TcpClientRunner : ITcpClientRunner
{
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorRepository _monitorRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ILogger<TcpClientRunner> _logger;

    public TcpClientRunner(IMonitorRepository monitorRepository, INotificationProducer notificationProducer, IMonitorHistoryRepository monitorHistoryRepository, ILogger<TcpClientRunner> logger)
    {
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorHistoryRepository = monitorHistoryRepository;
        _logger = logger;
    }

    public async Task<bool> CheckTcpAsync(MonitorTcp monitorTcp)
    {
        bool isConnected = false;
        int retries = 0;
        int retryIntervalMilliseconds = 3000;

        while (!isConnected && retries < monitorTcp.Retries)
        {
            try
            {
                isConnected = await MakeTcpCall(monitorTcp);
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorTcp.MonitorId,
                    Status = isConnected,
                    TimeStamp = DateTime.UtcNow,
                    StatusCode = 0,
                    ResponseMessage = $"Success to establish a connection to {monitorTcp.IP}:{monitorTcp.Port}",
                    ResponseTime = 0,
                    HttpVersion = ""
                };

                if (isConnected)
                {
                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
                    await _monitorRepository.UpdateMonitorStatus(monitorTcp.MonitorId, isConnected, 0);

                    if (!monitorTcp.LastStatus)
                    {
                        await _notificationProducer.HandleSuccessTcpNotifications(monitorTcp);
                    }
                }
                else
                {
                    retries++;
                    continue;
                }
            }
            catch (Exception)
            {
                retries++;
                // Wait for the specified interval before retrying
                Thread.Sleep(retryIntervalMilliseconds);
            }
        }

        if (!isConnected)
        {
            var monitorHistory = new MonitorHistory
            {
                MonitorId = monitorTcp.MonitorId,
                Status = isConnected,
                TimeStamp = DateTime.UtcNow,
                StatusCode = 0,
                ResponseMessage =
                    $"Failed to establish a connection to {monitorTcp.IP}:{monitorTcp.Port} after {monitorTcp.Retries} retries. Response: {monitorTcp.Response}",
                ResponseTime = 0,
                HttpVersion = ""
            };

            await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
            await _monitorRepository.UpdateMonitorStatus(monitorTcp.MonitorId, isConnected, 0);

            if (monitorTcp.LastStatus)
            {
                _logger.LogWarning("Failed to establish a connection to {monitorTcp.IP}:{monitorTcp.Port}, Response: {monitorTcp.Response}");
                await _notificationProducer.HandleFailedTcpNotifications(monitorTcp);
            }
        }

        return isConnected;
    }

    public async Task<bool> MakeTcpCall(MonitorTcp monitorTcp)
    {
        using var tcpClient = new TcpClient();
        using var cancellationTokenSource = new CancellationTokenSource();
        var timeoutMilliseconds = monitorTcp.Timeout * 1000;

        cancellationTokenSource.CancelAfter(timeoutMilliseconds * 1000);

        try
        {
            var connectTask = tcpClient.ConnectAsync(monitorTcp.IP, monitorTcp.Port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMilliseconds, cancellationTokenSource.Token));

            return completedTask == connectTask && tcpClient.Connected; // Return true if connected, false otherwise
        }
        catch (SocketException)
        {
            return false;
        }
        catch (Exception)
        {
            // Handle exceptions (such as invalid IP, port out of range, etc.)
            return false;
        }
    }
}