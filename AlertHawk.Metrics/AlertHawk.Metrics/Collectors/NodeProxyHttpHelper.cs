using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using k8s;

namespace AlertHawk.Metrics.Collectors;

/// <summary>
/// Fetches node proxy endpoints using KubernetesClientConfiguration (same auth as curl with Bearer token + CA).
/// Use this when the Kubernetes client's Connect* methods return 401 for node proxy.
/// </summary>
internal static class NodeProxyHttpHelper
{
    public static async Task<string> GetNodeStatsSummaryAsync(
        KubernetesClientConfiguration config,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        var url = $"{config.Host?.TrimEnd('/')}/api/v1/nodes/{nodeName}/proxy/stats/summary";

        using var handler = new HttpClientHandler();
        if (config.SkipTlsVerify)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        else if (config.SslCaCerts != null && config.SslCaCerts.Count > 0)
            handler.ServerCertificateCustomValidationCallback = (req, cert, chain, errors) =>
                Kubernetes.CertificateValidationCallBack(req, config.SslCaCerts!, cert as X509Certificate2 ?? new X509Certificate2(cert!), chain ?? new X509Chain(), errors);

        using var httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Auth: same as curl -H "Authorization: Bearer $TOKEN"
        var token = config.AccessToken;
        if (string.IsNullOrEmpty(token) && config.TokenProvider != null)
        {
            var authHeader = await config.TokenProvider.GetAuthenticationHeaderAsync(cancellationToken);
            token = authHeader?.Parameter;
        }
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
