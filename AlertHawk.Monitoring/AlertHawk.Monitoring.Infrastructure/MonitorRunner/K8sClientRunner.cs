using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using k8s;
using k8s.KubeConfigModels;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class K8sClientRunner : IK8sClientRunner
{
    private readonly ILogger<K8sClientRunner> _logger;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly int _retryIntervalMilliseconds = 6000;

    public K8sClientRunner(ILogger<K8sClientRunner> logger, IMonitorHistoryRepository monitorHistoryRepository,
        IMonitorRepository monitorRepository, INotificationProducer notificationProducer,
        IMonitorAlertRepository monitorAlertRepository, ISystemConfigurationRepository systemConfigurationRepository)
    {
        _logger = logger;
        _monitorHistoryRepository = monitorHistoryRepository;
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
    }

    [ExcludeFromCodeCoverage]
    public async Task CheckK8sAsync(MonitorK8s monitorK8s)
    {
        // Check if monitor execution is disabled (system maintenance mode)
        if (await _systemConfigurationRepository.IsMonitorExecutionDisabled())
        {
            _logger.LogInformation("Monitor execution is disabled. Skipping K8s monitor check for MonitorId: {MonitorId}", monitorK8s.MonitorId);
            return;
        }
        _logger.LogInformation("Checking K8s");
        int maxRetries = monitorK8s.Retries + 1;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var (succeeded, responseMessage, monitorHistory) = await CallK8S(monitorK8s);

                if (succeeded)
                {
                    _logger.LogInformation("K8s check succeeded");
                    await _monitorRepository.UpdateMonitorStatus(monitorK8s.MonitorId, succeeded, 0);
                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

                    await _monitorRepository.UpdateK8sMonitorNodeStatus(monitorK8s);

                    if (!monitorK8s.LastStatus)
                    {
                        await _notificationProducer.HandleSuccessK8sNotifications(monitorK8s, "Cluster is Up");
                        await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitorK8s.MonitorEnvironment);
                    }

                    break;
                }
                else
                {
                    _logger.LogInformation($"K8s check failed with message: {responseMessage}");
                    monitorHistory.ResponseMessage = responseMessage;
                    retryCount++;
                    Thread.Sleep(_retryIntervalMilliseconds);

                    if (retryCount == maxRetries)
                    {
                        await _monitorRepository.UpdateMonitorStatus(monitorK8s.MonitorId, succeeded,
                            0);
                        await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
                        await _monitorRepository.UpdateK8sMonitorNodeStatus(monitorK8s);

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
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("401") || httpEx.Message.Contains("Unauthorized"))
            {
                _logger.LogError("Kubernetes authentication failed (401): {message}", httpEx.Message);
                
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorK8s.MonitorId,
                    Status = false,
                    TimeStamp = DateTime.UtcNow,
                    ResponseMessage = $"Authentication failed (401): {httpEx.Message}"
                };

                // Save the authentication failure to history
                await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
                
                // Pause the monitor to prevent continuous authentication failures
                await _monitorRepository.PauseMonitor(monitorK8s.MonitorId, true);
                _logger.LogWarning("Monitor {monitorId} has been paused due to authentication failure", monitorK8s.MonitorId);

                // Send notification about the authentication failure and monitor pause
                await _notificationProducer.HandleFailedK8sNotifications(monitorK8s,
                    $"Authentication failed - Monitor paused: {httpEx.Message}");

                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory,
                    monitorK8s.MonitorEnvironment);

                // Break out of retry loop for authentication failures
                break;
            }
            catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                _logger.LogError("Kubernetes authentication failed: {message}", ex.Message);
                
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorK8s.MonitorId,
                    Status = false,
                    TimeStamp = DateTime.UtcNow,
                    ResponseMessage = $"Authentication failed: {ex.Message}"
                };

                // Save the authentication failure to history
                await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
                
                // Pause the monitor to prevent continuous authentication failures
                await _monitorRepository.PauseMonitor(monitorK8s.MonitorId, true);
                _logger.LogWarning("Monitor {monitorId} has been paused due to authentication failure", monitorK8s.MonitorId);

                // Send notification about the authentication failure and monitor pause
                await _notificationProducer.HandleFailedK8sNotifications(monitorK8s,
                    $"Authentication failed - Monitor paused: {ex.Message}");

                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory,
                    monitorK8s.MonitorEnvironment);

                // Break out of retry loop for authentication failures
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error connecting to kubernetes: {message}", ex.Message);
                retryCount++;
                
                var monitorHistory = new MonitorHistory
                {
                    MonitorId = monitorK8s.MonitorId,
                    Status = false,
                    TimeStamp = DateTime.UtcNow,
                    ResponseMessage = $"Failed to connect to Kubernetes cluster: {ex.Message}"
                };
                
                // Only save history and send notifications on final retry
                if (retryCount == maxRetries)
                {
                    await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
                    await _monitorRepository.UpdateMonitorStatus(monitorK8s.MonitorId, false, 0);
                    
                    await _notificationProducer.HandleFailedK8sNotifications(monitorK8s,
                        $"Failed to connect to Kubernetes cluster: {ex.Message}");

                    await _monitorAlertRepository.SaveMonitorAlert(monitorHistory,
                        monitorK8s.MonitorEnvironment);
                }
                else
                {
                    // Wait before retrying
                    Thread.Sleep(_retryIntervalMilliseconds);
                }
            }
        }
    }

    public async Task<(bool succeeded, string responseMessage, MonitorHistory monitorHistory)> CallK8S(MonitorK8s monitorK8s)
    {
        _logger.LogInformation("Loading configuration");
        var filePath = "kubeconfig/config.yaml";
        if (!string.IsNullOrEmpty(monitorK8s.KubeConfig))
        {
            _logger.LogInformation("Using provided kubeconfig");

            var fileBytes = Convert.FromBase64String(monitorK8s.KubeConfig); // Decode base64 string
            filePath = Path.Combine("kubeconfig", "config.yaml"); // Define file path

            _logger.LogInformation("Writing kubeconfig to file");

            Directory.CreateDirectory("kubeconfig"); // Ensure directory exists

            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes); // Write decoded bytes to file
            _logger.LogInformation("Wrote kubeconfig to file");
        }

        _logger.LogInformation("Building Kubernetes client");
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(filePath);
        var client = new Kubernetes(config);

        _logger.LogInformation("Fetching nodes from Kubernetes");

        // Fetch nodes from Kubernetes
        var nodes = await client.CoreV1.ListNodeAsync();

        List<K8sNodeStatusModel> nodeStatuses = new List<K8sNodeStatusModel>();

        foreach (var node in nodes.Items)
        {
            _logger.LogInformation($"Processing node {node.Metadata.Name}");
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
                    case "FrequentDockerRestart": nodeStatus.FrequentDockerRestart = isTrue; break;
                    case "FrequentContainerdRestart": nodeStatus.FrequentContainerdRestart = isTrue; break;
                    case "MemoryPressure": nodeStatus.MemoryPressure = isTrue; break;
                    case "DiskPressure": nodeStatus.DiskPressure = isTrue; break;
                    case "PIDPressure": nodeStatus.PIDPressure = isTrue; break;
                    case "Ready": nodeStatus.Ready = isTrue; break;
                }
            }

            _logger.LogInformation(
                $"Node {nodeStatus.NodeName} status: {JsonSerializer.Serialize(nodeStatus)}");

            nodeStatuses.Add(nodeStatus);
        }
                
        monitorK8s.MonitorK8sNodes = new List<K8sNodeStatusModel>();
        monitorK8s.MonitorK8sNodes = nodeStatuses;

        bool succeeded = true;
        var responseMessage = new StringBuilder();

        foreach (var node in nodeStatuses)
        {
            if (node.Ready == false)
            {
                _logger.LogInformation($"Node {node.NodeName} is not ready");
                responseMessage.AppendLine($"Node {node.NodeName} is not ready");
                succeeded = false;
            }

            if (node.DiskPressure)
            {
                _logger.LogInformation($"Node {node.NodeName} has disk pressure");
                responseMessage.AppendLine($"Node {node.NodeName} has disk pressure");
                succeeded = false;
            }

            if (node.MemoryPressure)
            {
                _logger.LogInformation($"Node {node.NodeName} has memory pressure");
                responseMessage.AppendLine($"Node {node.NodeName} has memory pressure");
                succeeded = false;
            }

            if (node.ContainerRuntimeProblem)
            {
                _logger.LogInformation($"Node {node.NodeName} has container runtime problem");
                responseMessage.AppendLine($"Node {node.NodeName} has container runtime problem");
                succeeded = false;
            }

            if (node.FilesystemCorruptionProblem)
            {
                _logger.LogInformation($"Node {node.NodeName} has filesystem corruption problem");
                responseMessage.AppendLine($"Node {node.NodeName} has filesystem corruption problem");
                succeeded = false;
            }

            if (node.KubeletProblem)
            {
                _logger.LogInformation($"Node {node.NodeName} has kubelet problem");
                responseMessage.AppendLine($"Node {node.NodeName} has kubelet problem");
                succeeded = false;
            }

            if (node.KernelDeadlock)
            {
                _logger.LogInformation($"Node {node.NodeName} has kernel deadlock");
                responseMessage.AppendLine($"Node {node.NodeName} has kernel deadlock");
                succeeded = false;
            }

            if (node.FrequentUnregisterNetDevice)
            {
                _logger.LogInformation($"Node {node.NodeName} has frequent unregister net device");
                responseMessage.AppendLine($"Node {node.NodeName} has frequent unregister net device");
                succeeded = false;
            }

            if (node.ReadonlyFilesystem)
            {
                _logger.LogInformation($"Node {node.NodeName} has readonly filesystem");
                responseMessage.AppendLine($"Node {node.NodeName} has readonly filesystem");
                succeeded = false;
            }

            if (node.FrequentKubeletRestart)
            {
                _logger.LogInformation($"Node {node.NodeName} has frequent kubelet restart");
                responseMessage.AppendLine($"Node {node.NodeName} has frequent kubelet restart");
                succeeded = false;
            }
                    
            if (node.FrequentDockerRestart)
            {
                _logger.LogInformation($"Node {node.NodeName} has frequent docker restart");
                responseMessage.AppendLine($"Node {node.NodeName} has frequent docker restart");
                succeeded = false;
            }

            if (node.FrequentContainerdRestart)
            {
                _logger.LogInformation($"Node {node.NodeName} has frequent containerd restart");
                responseMessage.AppendLine($"Node {node.NodeName} has frequent containerd restart");
                succeeded = false;
            }

            if (node.PIDPressure)
            {
                _logger.LogInformation($"Node {node.NodeName} has PID pressure");
                responseMessage.AppendLine($"Node {node.NodeName} has PID pressure");
                succeeded = false;
            }
        }

        var responseMessageString = responseMessage.ToString();
        var monitorHistory = new MonitorHistory
        {
            MonitorId = monitorK8s.MonitorId,
            Status = succeeded,
            TimeStamp = DateTime.UtcNow,
            ResponseMessage = responseMessageString
        };
        return (succeeded, responseMessageString, monitorHistory);
    }
}