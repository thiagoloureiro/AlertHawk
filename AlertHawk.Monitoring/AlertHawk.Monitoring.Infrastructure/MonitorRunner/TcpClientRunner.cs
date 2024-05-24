using System.Net.Sockets;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class TcpClientRunner : ITcpClientRunner
{
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorRepository _monitorRepository;

    public TcpClientRunner(IMonitorRepository monitorRepository, INotificationProducer notificationProducer)
    {
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
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
                isConnected = MakeTcpCall(monitorTcp);

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
                    await _monitorRepository.SaveMonitorHistory(monitorHistory);
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
            catch (Exception err)
            {
                retries++;
                // Wait for the specified interval before retrying
                Console.WriteLine(err.Message);
                Console.WriteLine($"Retrying in {retryIntervalMilliseconds} milliseconds...");
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

            await _monitorRepository.SaveMonitorHistory(monitorHistory);
            await _monitorRepository.UpdateMonitorStatus(monitorTcp.MonitorId, isConnected, 0);

            if (monitorTcp.LastStatus)
            {
                await _notificationProducer.HandleFailedTcpNotifications(monitorTcp);
            }
        }

        return isConnected;
    }

    public bool MakeTcpCall(MonitorTcp monitorTcp)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var timeoutInSeconds = monitorTcp.Timeout;

            var task = tcpClient.ConnectAsync(monitorTcp.IP, monitorTcp.Port);
            if (Task.WaitAny(new Task[] { task }, timeoutInSeconds * 1000) == -1)
            {
                // Timeout, connection attempt was not successful within the given timeframe
                return false;
            }

            return tcpClient.Connected; // Return true if connected, false otherwise
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