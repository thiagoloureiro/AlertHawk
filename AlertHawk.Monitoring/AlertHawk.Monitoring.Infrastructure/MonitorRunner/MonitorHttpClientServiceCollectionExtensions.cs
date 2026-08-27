using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

[ExcludeFromCodeCoverage]
public static class MonitorHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddMonitorHttpClients(this IServiceCollection services)
    {
        void ConfigureClient(HttpClient client)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AlertHawk/1.0.1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "br");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        }

        services.AddHttpClient(HttpClientRunner.DefaultHttpClientName, ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateDefaultHandler);

        services.AddHttpClient(HttpClientRunner.InsecureHttpClientName, ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateInsecureHandler);

        return services;
    }

    private static HttpMessageHandler CreateDefaultHandler()
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxAutomaticRedirections = 50,
            AllowAutoRedirect = true
        };
    }

    private static HttpMessageHandler CreateInsecureHandler()
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxAutomaticRedirections = 50,
            AllowAutoRedirect = true,
            SslOptions =
            {
                RemoteCertificateValidationCallback = HttpClientRunner.InsecureCertificateValidationCallback
            }
        };
    }
}
