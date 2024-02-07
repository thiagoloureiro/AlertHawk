using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class TcpClientRunner: ITcpClientRunner
{
    private readonly IMonitorRepository _monitorRepository;

    private readonly IPublishEndpoint _publishEndpoint;

    public TcpClientRunner(IMonitorRepository monitorRepository, IPublishEndpoint publishEndpoint)
    {
        _monitorRepository = monitorRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> CheckTcpAsync(string ipAddress, int port, int maxRetries, int retryIntervalMilliseconds)
    {
        bool isConnected = false;
        int retries = 0;

        while (!isConnected && retries < maxRetries)
        {
            try
            {
                using TcpClient client = new TcpClient();
                // Set a short timeout for the connection attempt
                client.ReceiveTimeout = 1000;
                client.SendTimeout = 1000;

                // Attempt to connect to the IP address and port
                await client.ConnectAsync(ipAddress, port);
                isConnected = true;
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
            throw new Exception($"Failed to establish a connection to {ipAddress}:{port} after {maxRetries} retries.");
        }

        return isConnected;
    }
}