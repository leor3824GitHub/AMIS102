using System.Net;

namespace AMIS.Playground.Blazor.Services.Api;

/// <summary>
/// Retries transient failures for idempotent requests.
/// </summary>
internal sealed class ApiRetryHandler(ILogger<ApiRetryHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    ];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!IsRetryEligible(request.Method))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var attempt = 0;
        while (attempt <= RetryDelays.Length)
        {
            try
            {
                using var retryRequest = await CloneHttpRequestMessageAsync(request, cancellationToken);
                var response = await base.SendAsync(retryRequest, cancellationToken);
                if (!IsTransientStatus(response.StatusCode) || attempt >= RetryDelays.Length)
                {
                    return response;
                }

                logger.LogWarning("Transient HTTP status {StatusCode} on attempt {Attempt} for {Method} {Path}. Retrying in {DelayMs} ms...",
                    (int)response.StatusCode,
                    attempt + 1,
                    request.Method,
                    request.RequestUri?.PathAndQuery,
                    RetryDelays[attempt].TotalMilliseconds);
                response.Dispose();
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < RetryDelays.Length)
            {
                logger.LogWarning(ex,
                    "Transient HTTP exception on attempt {Attempt} for {Method} {Path}. Retrying in {DelayMs} ms...",
                    attempt + 1,
                    request.Method,
                    request.RequestUri?.PathAndQuery,
                    RetryDelays[attempt].TotalMilliseconds);
            }

            await Task.Delay(RetryDelays[attempt], cancellationToken);
            attempt++;
        }

        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }

    private static bool IsRetryEligible(HttpMethod method) =>
        method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;

    private static bool IsTransientStatus(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static bool IsTransientException(Exception ex) =>
        ex is HttpRequestException || ex is TaskCanceledException;
}

