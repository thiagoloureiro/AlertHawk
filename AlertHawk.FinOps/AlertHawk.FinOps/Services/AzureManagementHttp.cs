using System;
using System.Net.Http;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Shared <see cref="HttpClient"/> for Azure Resource Manager / Cost Management REST calls.
    /// Avoids socket exhaustion from per-call <c>new HttpClient()</c>. Auth must be set per request
    /// (never on <see cref="HttpClient.DefaultRequestHeaders"/>) because this instance is shared.
    /// </summary>
    internal static class AzureManagementHttp
    {
        internal static HttpClient Shared { get; } = Create();

        private static HttpClient Create()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            return client;
        }
    }
}
