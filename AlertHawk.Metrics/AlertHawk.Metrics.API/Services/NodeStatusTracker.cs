using System.Collections.Concurrent;

namespace AlertHawk.Metrics.API.Services;

public class NodeStatusTracker
{
    private readonly ConcurrentDictionary<string, NodeStatus> _nodeStatuses = new();

    public class NodeStatus
    {
        public bool? IsReady { get; set; }
        public bool? HasMemoryPressure { get; set; }
        public bool? HasDiskPressure { get; set; }
        public bool? HasPidPressure { get; set; }
    }

    public bool HasStatusChanged(string nodeKey, bool? isReady, bool? hasMemoryPressure, bool? hasDiskPressure, bool? hasPidPressure, out NodeStatus? previousStatus)
    {
        previousStatus = null;
        
        if (!_nodeStatuses.TryGetValue(nodeKey, out var currentStatus))
        {
            // First time we see this node, store the status but don't consider it a change
            _nodeStatuses[nodeKey] = new NodeStatus
            {
                IsReady = isReady,
                HasMemoryPressure = hasMemoryPressure,
                HasDiskPressure = hasDiskPressure,
                HasPidPressure = hasPidPressure
            };
            return false;
        }

        previousStatus = new NodeStatus
        {
            IsReady = currentStatus.IsReady,
            HasMemoryPressure = currentStatus.HasMemoryPressure,
            HasDiskPressure = currentStatus.HasDiskPressure,
            HasPidPressure = currentStatus.HasPidPressure
        };

        // Check if any status has changed
        var hasChanged = currentStatus.IsReady != isReady ||
                        currentStatus.HasMemoryPressure != hasMemoryPressure ||
                        currentStatus.HasDiskPressure != hasDiskPressure ||
                        currentStatus.HasPidPressure != hasPidPressure;

        if (hasChanged)
        {
            // Update the stored status
            _nodeStatuses[nodeKey] = new NodeStatus
            {
                IsReady = isReady,
                HasMemoryPressure = hasMemoryPressure,
                HasDiskPressure = hasDiskPressure,
                HasPidPressure = hasPidPressure
            };
        }

        return hasChanged;
    }

    public string GetNodeKey(string nodeName, string? clusterName)
    {
        return string.IsNullOrWhiteSpace(clusterName) 
            ? nodeName 
            : $"{clusterName}:{nodeName}";
    }
}
