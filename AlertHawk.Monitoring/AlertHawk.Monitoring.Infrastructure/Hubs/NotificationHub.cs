using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Hubs;

[ExcludeFromCodeCoverage]
public class NotificationHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task JoinMonitorGroup(int monitorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Monitor_{monitorId}");
    }

    public async Task LeaveMonitorGroup(int monitorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Monitor_{monitorId}");
    }

    public async Task JoinEnvironmentGroup(int environment)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Environment_{environment}");
    }

    public async Task LeaveEnvironmentGroup(int environment)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Environment_{environment}");
    }

    public async Task JoinRegionGroup(int region)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Region_{region}");
    }

    public async Task LeaveRegionGroup(int region)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Region_{region}");
    }
}
