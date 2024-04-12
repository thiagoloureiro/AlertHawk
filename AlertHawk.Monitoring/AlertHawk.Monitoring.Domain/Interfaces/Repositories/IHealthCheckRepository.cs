namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IHealthCheckRepository
{
    Task<bool> CheckHealthAsync();
}