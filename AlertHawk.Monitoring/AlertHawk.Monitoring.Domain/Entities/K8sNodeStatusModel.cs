namespace AlertHawk.Monitoring.Domain.Entities;

public class K8sNodeStatusModel
{
    public string NodeName { get; set; }

    // Node Conditions as Properties
    public bool ContainerRuntimeProblem { get; set; }
    public bool KernelDeadlock { get; set; }
    public bool KubeletProblem { get; set; }
    public bool FrequentUnregisterNetDevice { get; set; }
    public bool FilesystemCorruptionProblem { get; set; }
    public bool ReadonlyFilesystem { get; set; }
    public bool FrequentKubeletRestart { get; set; }
    public bool VMEventScheduled { get; set; }
    public bool FrequentDockerRestart { get; set; }
    public bool FrequentContainerdRestart { get; set; }
    public bool MemoryPressure { get; set; }
    public bool DiskPressure { get; set; }
    public bool PIDPressure { get; set; }
    public bool Ready { get; set; }
}