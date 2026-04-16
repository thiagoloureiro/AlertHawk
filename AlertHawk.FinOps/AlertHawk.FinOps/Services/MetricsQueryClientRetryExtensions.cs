using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    internal static class MetricsQueryClientRetryExtensions
    {
        public static Task<Response<MetricsQueryResult>> QueryResourceWithRetryAsync(
            this MetricsQueryClient client,
            string resourceId,
            IEnumerable<string> metricNames,
            MetricsQueryOptions options,
            CancellationToken cancellationToken = default) =>
            AzureThrottledRequestRetry.ExecuteAsync(
                () => client.QueryResourceAsync(resourceId, metricNames, options, cancellationToken),
                cancellationToken);
    }
}
