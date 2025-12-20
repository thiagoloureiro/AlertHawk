namespace AlertHawk.Metrics.API.Producers;

public interface INotificationProducer
{
    Task SendNodeStatusNotification(
        string nodeName,
        string? clusterName,
        string? clusterEnvironment,
        bool? isReady,
        bool? hasMemoryPressure,
        bool? hasDiskPressure,
        bool? hasPidPressure,
        bool success);
}