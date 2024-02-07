using System.Net.Sockets;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class TcpClientRunner : ITcpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;

    private readonly IPublishEndpoint _publishEndpoint;

    public TcpClientRunner(IMonitorRepository monitorRepository, IPublishEndpoint publishEndpoint)
    {
        _monitorRepository = monitorRepository;
        _publishEndpoint = publishEndpoint;
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
                using TcpClient client = new TcpClient();
                // Set a short timeout for the connection attempt
                client.ReceiveTimeout = monitorTcp.Timeout;
                client.SendTimeout = monitorTcp.Timeout;

                // Attempt to connect to the IP address and port
                await client.ConnectAsync(monitorTcp.IP, monitorTcp.Port);
                isConnected = true;

                if (!monitorTcp.LastStatus)
                {
                    await HandleSuccessNotifications(monitorTcp);
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
                await HandleFailedNotifications(monitorTcp);
            }

            throw new Exception(
                $"Failed to establish a connection to {monitorTcp.IP}:{monitorTcp.Port} after {monitorTcp.Retries} retries.");
        }

        return isConnected;
    }

    private async Task HandleSuccessNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        Console.WriteLine(
            $"sending success notification calling {monitorTcp.IP} Port: {monitorTcp.Port},");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Success calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
            });
        }
    }

    private async Task HandleFailedNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        Console.WriteLine(
            $"sending notification Error calling {monitorTcp.IP} Port: {monitorTcp.Port}, Response: {monitorTcp.Response}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Error calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
            });
        }
    }
}