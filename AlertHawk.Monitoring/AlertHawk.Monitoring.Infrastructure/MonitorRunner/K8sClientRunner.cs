using System.Text.Json;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using k8s;
using k8s.KubeConfigModels;
using Microsoft.Extensions.Logging;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class K8sClientRunner : IK8sClientRunner
{
    private readonly ILogger<K8sClientRunner> _logger;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly int _retryIntervalMilliseconds = 6000;

    public K8sClientRunner(ILogger<K8sClientRunner> logger, IMonitorHistoryRepository monitorHistoryRepository,
        IMonitorRepository monitorRepository, INotificationProducer notificationProducer,
        IMonitorAlertRepository monitorAlertRepository)
    {
        _logger = logger;
        _monitorHistoryRepository = monitorHistoryRepository;
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
    }

    public async Task CheckK8sAsync(MonitorK8s monitorK8s)
    {
        int maxRetries = monitorK8s.Retries + 1;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                string kubeconfigPath = monitorK8s.KubeConfig;
                var k8sOject = JsonSerializer.Deserialize<K8SConfiguration>(kubeconfigPath);

                var config = KubernetesClientConfiguration.BuildConfigFromConfigObject(k8sOject);
                var client = new Kubernetes(config);

                // Fetch nodes from Kubernetes
                var nodes = await client.CoreV1.ListNodeAsync();

                List<K8sNodeStatusModel> nodeStatuses = new List<K8sNodeStatusModel>();

                foreach (var node in nodes.Items)
                {
                    var nodeStatus = new K8sNodeStatusModel
                    {
                        NodeName = node.Metadata.Name
                    };

                    // Map Kubernetes node conditions to model properties
                    foreach (var condition in node.Status.Conditions)
                    {
                        bool isTrue = condition.Status == "True";

                        switch (condition.Type)
                        {
                            case "ContainerRuntimeProblem": nodeStatus.ContainerRuntimeProblem = isTrue; break;
                            case "KernelDeadlock": nodeStatus.KernelDeadlock = isTrue; break;
                            case "KubeletProblem": nodeStatus.KubeletProblem = isTrue; break;
                            case "FrequentUnregisterNetDevice": nodeStatus.FrequentUnregisterNetDevice = isTrue; break;
                            case "FilesystemCorruptionProblem": nodeStatus.FilesystemCorruptionProblem = isTrue; break;
                            case "ReadonlyFilesystem": nodeStatus.ReadonlyFilesystem = isTrue; break;
                            case "FrequentKubeletRestart": nodeStatus.FrequentKubeletRestart = isTrue; break;
                            case "VMEventScheduled": nodeStatus.VMEventScheduled = isTrue; break;
                            case "FrequentDockerRestart": nodeStatus.FrequentDockerRestart = isTrue; break;
                            case "FrequentContainerdRestart": nodeStatus.FrequentContainerdRestart = isTrue; break;
                            case "MemoryPressure": nodeStatus.MemoryPressure = isTrue; break;
                            case "DiskPressure": nodeStatus.DiskPressure = isTrue; break;
                            case "PIDPressure": nodeStatus.PIDPressure = isTrue; break;
                            case "Ready": nodeStatus.Ready = isTrue; break;
                        }
                    }

                    nodeStatuses.Add(nodeStatus);
                }

                bool succeeded = true;
                var responseMessage = "";

                foreach (var node in nodeStatuses)
                {
                    if (node.Ready == false)
                    {
                        _logger.LogInformation($"Node {node.NodeName} is not ready");
                        responseMessage += $"Node {node.NodeName} is not ready\n";
                        succeeded = false;
                    }

                    if (node.DiskPressure)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has disk pressure");
                        responseMessage += $"Node {node.NodeName} has disk pressure\n";
                        succeeded = false;
                    }

                    if (node.MemoryPressure)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has memory pressure");
                        responseMessage += $"Node {node.NodeName} has memory pressure\n";
                        succeeded = false;
                    }

                    if (node.ContainerRuntimeProblem)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has container runtime problem");
                        responseMessage += $"Node {node.NodeName} has container runtime problem\n";
                        succeeded = false;
                    }

                    if (node.FilesystemCorruptionProblem)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has filesystem corruption problem");
                        responseMessage += $"Node {node.NodeName} has filesystem corruption problem\n";
                        succeeded = false;
                    }

                    if (node.KubeletProblem)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has kubelet problem");
                        responseMessage += $"Node {node.NodeName} has kubelet problem\n";
                        succeeded = false;
                    }

                    if (node.KernelDeadlock)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has kernel deadlock");
                        responseMessage += $"Node {node.NodeName} has kernel deadlock\n";
                        succeeded = false;
                    }

                    if (node.FrequentUnregisterNetDevice)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has frequent unregister net device");
                        responseMessage += $"Node {node.NodeName} has frequent unregister net device\n";
                        succeeded = false;
                    }

                    if (node.ReadonlyFilesystem)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has readonly filesystem");
                        responseMessage += $"Node {node.NodeName} has readonly filesystem\n";
                        succeeded = false;
                    }

                    if (node.FrequentKubeletRestart)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has frequent kubelet restart");
                        responseMessage += $"Node {node.NodeName} has frequent kubelet restart\n";
                        succeeded = false;
                    }

                    if (node.VMEventScheduled)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has VM event scheduled");
                        responseMessage += $"Node {node.NodeName} has VM event scheduled\n";
                        succeeded = false;
                    }

                    if (node.FrequentDockerRestart)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has frequent docker restart");
                        responseMessage += $"Node {node.NodeName} has frequent docker restart\n";
                        succeeded = false;
                    }

                    if (node.FrequentContainerdRestart)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has frequent containerd restart");
                        responseMessage += $"Node {node.NodeName} has frequent containerd restart\n";
                        succeeded = false;
                    }

                    if (node.PIDPressure)
                    {
                        _logger.LogInformation($"Node {node.NodeName} has PID pressure");
                        responseMessage += $"Node {node.NodeName} has PID pressure\n";
                        succeeded = false;
                    }
                }

                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorK8s.MonitorId,
                    Status = succeeded,
                    TimeStamp = DateTime.UtcNow,
                    ResponseMessage = responseMessage
                };

                if (succeeded)
                {
                    await _monitorRepository.UpdateMonitorStatus(monitorK8s.MonitorId, succeeded, 0);
                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                    if (!monitorK8s.LastStatus)
                    {
                        await _notificationProducer.HandleSuccessK8sNotifications(monitorK8s, "Cluster is Up");
                        await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitorK8s.MonitorEnvironment);
                    }

                    break;
                }
                else
                {
                    monitorHistory.ResponseMessage = responseMessage;
                    retryCount++;
                    Thread.Sleep(_retryIntervalMilliseconds);

                    if (retryCount == maxRetries)
                    {
                        await _monitorRepository.UpdateMonitorStatus(monitorK8s.MonitorId, succeeded,
                            0);
                        await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                        // only send notification when goes from online into offline to avoid flood
                        if (monitorK8s.LastStatus)
                        {
                            await _notificationProducer.HandleFailedK8sNotifications(monitorK8s,
                                responseMessage);

                            await _monitorAlertRepository.SaveMonitorAlert(monitorHistory,
                                monitorK8s.MonitorEnvironment);

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }
    }
}