using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface IHttpClientRunner
{
    Task CheckUrlsAsync(MonitorHttp monitorHttp);

    Task<HttpResponseMessage> MakeHttpClientCall(MonitorHttp monitorHttp);
    MonitorHttpHeaders CheckHttpHeaders(HttpResponseMessage response);
}