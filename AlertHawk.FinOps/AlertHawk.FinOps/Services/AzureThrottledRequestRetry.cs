using Azure;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Retries Azure management and Cost Management calls when throttled or temporarily unavailable.
    /// Cost Management query POSTs are serialized process-wide to reduce concurrent 429s.
    /// </summary>
    internal static class AzureThrottledRequestRetry
    {
        private const int MaxAttempts = 10;
        private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(180);

        /// <summary>
        /// Cost Management Query is heavily rate-limited; serialize POSTs across analysis runs.
        /// </summary>
        private static readonly SemaphoreSlim CostManagementGate = new(1, 1);

        private static bool IsRetriableStatus(int statusCode) =>
            statusCode == (int)HttpStatusCode.TooManyRequests
            || statusCode == (int)HttpStatusCode.ServiceUnavailable
            || statusCode == (int)HttpStatusCode.GatewayTimeout
            || statusCode == 502; // Bad Gateway - often transient at the edge

        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (RequestFailedException ex) when (IsRetriableStatus(ex.Status) && attempt < MaxAttempts)
                {
                    var delay = ComputeDelay(attempt, TryGetRetryAfterFromSdk(ex));
                    Console.WriteLine(
                        $"⚠️ Azure SDK throttled ({ex.Status}); waiting {delay.TotalSeconds:0.#}s before retry {attempt + 1}/{MaxAttempts}...");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Azure request did not return a value after retries.");
        }

        /// <summary>
        /// POST with fresh <see cref="HttpContent"/> per attempt (required after failed sends).
        /// </summary>
        public static async Task<HttpResponseMessage> SendPostWithRetryAsync(
            HttpClient httpClient,
            string requestUri,
            Func<HttpContent> contentFactory,
            CancellationToken cancellationToken = default)
        {
            return await SendPostWithRetryCoreAsync(
                httpClient,
                requestUri,
                bearerToken: null,
                contentFactory,
                useCostManagementGate: false,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cost Management Query POST — process-wide mutex + longer backoff on 429.
        /// Uses per-request Bearer auth (safe with a shared <see cref="HttpClient"/>).
        /// Honors Retry-After and x-ms-ratelimit-*-retry-after headers.
        /// </summary>
        public static async Task<HttpResponseMessage> SendCostManagementPostWithRetryAsync(
            HttpClient httpClient,
            string requestUri,
            string bearerToken,
            Func<HttpContent> contentFactory,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);

            return await SendPostWithRetryCoreAsync(
                httpClient,
                requestUri,
                bearerToken,
                contentFactory,
                useCostManagementGate: true,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> SendPostWithRetryCoreAsync(
            HttpClient httpClient,
            string requestUri,
            string? bearerToken,
            Func<HttpContent> contentFactory,
            bool useCostManagementGate,
            CancellationToken cancellationToken)
        {
            if (useCostManagementGate)
            {
                await CostManagementGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            try
            {
                HttpResponseMessage? response = null;
                for (var attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    if (response != null)
                    {
                        response.Dispose();
                        response = null;
                    }

                    if (bearerToken != null)
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                        request.Content = contentFactory();
                        response = await httpClient
                            .SendAsync(request, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        using var content = contentFactory();
                        response = await httpClient
                            .PostAsync(requestUri, content, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    var status = (int)response.StatusCode;
                    if (response.IsSuccessStatusCode || !IsRetriableStatus(status) || attempt == MaxAttempts)
                    {
                        if (!response.IsSuccessStatusCode && IsRetriableStatus(status) && attempt == MaxAttempts)
                        {
                            Console.WriteLine(
                                $"❌ Azure HTTP {(HttpStatusCode)status} after {MaxAttempts} attempts — giving up for this request.");
                        }

                        return response;
                    }

                    var retryAfter = TryGetRetryAfterFromHttp(response);
                    var delay = ComputeDelay(attempt, retryAfter, costManagement: useCostManagementGate);
                    Console.WriteLine(
                        $"⚠️ Azure HTTP {(HttpStatusCode)status}; waiting {delay.TotalSeconds:0.#}s before retry {attempt + 1}/{MaxAttempts}" +
                        (retryAfter.HasValue ? " (from rate-limit header)..." : "..."));

                    response.Dispose();
                    response = null;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                throw new InvalidOperationException("HTTP POST did not return a response after retries.");
            }
            finally
            {
                if (useCostManagementGate)
                {
                    CostManagementGate.Release();
                }
            }
        }

        private static TimeSpan? TryGetRetryAfterFromHttp(HttpResponseMessage response)
        {
            if (response.Headers.RetryAfter?.Delta is { } delta)
            {
                return ClampDelay(delta);
            }

            if (response.Headers.RetryAfter?.Date is { } date)
            {
                var until = date - DateTimeOffset.UtcNow;
                if (until > TimeSpan.Zero)
                {
                    return ClampDelay(until);
                }
            }

            // Standard Retry-After: seconds
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var parsed = TryParseRetryAfterSeconds(values.FirstOrDefault());
                if (parsed.HasValue)
                {
                    return ClampDelay(parsed.Value);
                }
            }

            // Cost Management / ARM custom headers, e.g.
            // x-ms-ratelimit-microsoft.costmanagement-qps-retry-after
            // x-ms-ratelimit-microsoft.costmanagement-entity-requests-retry-after
            foreach (var header in response.Headers)
            {
                if (!header.Key.Contains("retry-after", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parsed = TryParseRetryAfterSeconds(header.Value.FirstOrDefault());
                if (parsed.HasValue)
                {
                    return ClampDelay(parsed.Value);
                }
            }

            return null;
        }

        private static TimeSpan? TryParseRetryAfterSeconds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(Math.Max(1, seconds));
            }

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var secondsD))
            {
                return TimeSpan.FromSeconds(Math.Max(1, secondsD));
            }

            return null;
        }

        private static TimeSpan? TryGetRetryAfterFromSdk(RequestFailedException _)
        {
            // Azure.Core response header shapes vary by package version; use exponential backoff only.
            return null;
        }

        private static TimeSpan ComputeDelay(int attempt, TimeSpan? retryAfter, bool costManagement = false)
        {
            if (retryAfter.HasValue)
            {
                // Cost Management often needs the full Retry-After; add small jitter so we don't stampede.
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(250, 1500));
                return ClampDelay(retryAfter.Value + jitter);
            }

            // Cost Management default backoff is steeper (often needs 20–60s+ between queries).
            var baseDelay = costManagement ? TimeSpan.FromSeconds(15) : BaseDelay;
            var maxDelay = costManagement ? TimeSpan.FromSeconds(300) : MaxDelay;

            var exp = TimeSpan.FromTicks(
                Math.Min(maxDelay.Ticks, baseDelay.Ticks * (1L << Math.Min(attempt - 1, 5))));
            var jitterMs = Random.Shared.Next(0, costManagement ? 2000 : 500);
            return exp + TimeSpan.FromMilliseconds(jitterMs);
        }

        private static TimeSpan ClampDelay(TimeSpan d)
        {
            if (d < TimeSpan.FromSeconds(1))
            {
                return TimeSpan.FromSeconds(1);
            }

            // Allow up to 5 minutes when Azure asks for a long Retry-After.
            var max = TimeSpan.FromSeconds(300);
            return d > max ? max : d;
        }
    }
}
