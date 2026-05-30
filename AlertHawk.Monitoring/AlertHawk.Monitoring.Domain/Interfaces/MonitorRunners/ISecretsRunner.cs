namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface ISecretsRunner
{
    Task CheckSecretsAsync();
}
