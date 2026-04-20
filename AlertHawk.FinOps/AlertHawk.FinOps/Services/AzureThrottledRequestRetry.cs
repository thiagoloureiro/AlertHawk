using Azure;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Retries Azure management and monitor calls when throttled or temporarily unavailable.
    /// </summary>
    internal static class AzureThrottledRequestRetry
    {
        private const int MaxAttempts = 8;
        private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(120);

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
            HttpResponseMessage? response = null;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }

                using var content = contentFactory();
                response = await httpClient
                    .PostAsync(requestUri, content, cancellationToken)
                    .ConfigureAwait(false);

                var status = (int)response.StatusCode;
                if (response.IsSuccessStatusCode || !IsRetriableStatus(status) || attempt == MaxAttempts)
                {
                    return response;
                }

                var delay = ComputeDelay(attempt, TryGetRetryAfterFromHttp(response));
                response.Dispose();
                response = null;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidOperationException("HTTP POST did not return a response after retries.");
        }

        private static TimeSpan? TryGetRetryAfterFromHttp(HttpResponseMessage response)
        {
            if (response.Headers.RetryAfter?.Delta is { } delta)
            {
                return ClampDelay(delta);
            }

            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var raw = values.FirstOrDefault();
                if (int.TryParse(raw, out var seconds))
                {
                    return ClampDelay(TimeSpan.FromSeconds(seconds));
                }
            }

            return null;
        }

        private static TimeSpan? TryGetRetryAfterFromSdk(RequestFailedException _)
        {
            // Azure.Core response header shapes vary by package version; use exponential backoff only.
            return null;
        }

        private static TimeSpan ComputeDelay(int attempt, TimeSpan? retryAfter)
        {
            if (retryAfter.HasValue)
            {
                return retryAfter.Value;
            }

            var exp = TimeSpan.FromTicks(
                Math.Min(MaxDelay.Ticks, BaseDelay.Ticks * (1L << Math.Min(attempt - 1, 6))));
            var jitterMs = Random.Shared.Next(0, 500);
            return exp + TimeSpan.FromMilliseconds(jitterMs);
        }

        private static TimeSpan ClampDelay(TimeSpan d)
        {
            if (d < TimeSpan.FromSeconds(1))
            {
                return TimeSpan.FromSeconds(1);
            }

            return d > MaxDelay ? MaxDelay : d;
        }
    }
}
