namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IHealthCheckService
{
    Task<bool> CheckHealthAsync();
}