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

                await _monitorRepository.SaveMonitorHistory(monitorHistory);

                if (!monitorTcp.LastStatus)
                {
                    await _notificationProducer.HandleSuccessTcpNotifications(monitorTcp);
                }
            }
            catch (SocketException err)
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
            if (monitorTcp.LastStatus)
            {
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorTcp.MonitorId,
                    Status = isConnected,
                    TimeStamp = DateTime.UtcNow,
                    StatusCode = 0,
                    ResponseMessage = $"Failed to establish a connection to {monitorTcp.IP}:{monitorTcp.Port} after {monitorTcp.Retries} retries.",
                    ResponseTime = 0,
                    HttpVersion = ""
                };

                await _monitorRepository.SaveMonitorHistory(monitorHistory);
                
                await _notificationProducer.HandleFailedTcpNotifications(monitorTcp);
            }

            throw new Exception(
                $"Failed to establish a connection to {monitorTcp.IP}:{monitorTcp.Port} after {monitorTcp.Retries} retries.");
        }

        return isConnected;
    }

    public async Task<bool> MakeTcpCall(MonitorTcp monitorTcp)
    {
        try
        {
            CancellationToken cancellationToken = new CancellationToken();
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(monitorTcp.IP, monitorTcp.Port, cancellationToken);
            await connectTask.AsTask().WaitAsync(TimeSpan.FromSeconds(monitorTcp.Timeout), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var isConnected = true;
            return isConnected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}