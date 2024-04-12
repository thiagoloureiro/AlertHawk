using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using EasyMemoryCache.Memorycache;

namespace AlertHawk.Monitoring.Domain.Classes;

public class HealthCheckService : IHealthCheckService
{
    private readonly IHealthCheckRepository _healthCheckRepository;
    private readonly ICaching _caching;

    public HealthCheckService(IHealthCheckRepository healthCheckRepository, ICaching caching)
    {
        _healthCheckRepository = healthCheckRepository;
        _caching = caching;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await _caching.SetValueToCacheAsync("CacheKey", "CacheValue");
            var result = await _caching.GetValueFromCacheAsync<string>("CacheKey");
            var result1 = await _healthCheckRepository.CheckHealthAsync();
            return result1 && result == "CacheValue";
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            return false;
        }
    }
}