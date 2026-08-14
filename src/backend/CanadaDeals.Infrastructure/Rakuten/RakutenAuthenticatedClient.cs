using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenRequestGate(IOptions<RakutenOptions> options, TimeProvider clock)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _minimumInterval = TimeSpan.FromMilliseconds(options.Value.MinimumRequestIntervalMilliseconds);
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = clock.GetUtcNow();
            var delay = _nextRequestAt - now;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken);
            _nextRequestAt = clock.GetUtcNow().Add(_minimumInterval);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class RakutenAuthenticatedClient(
    HttpClient httpClient,
    IRakutenAccessTokenProvider tokens,
    RakutenRequestGate requestGate,
    ILogger<RakutenAuthenticatedClient> logger)
{
    public async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await requestGate.WaitAsync(cancellationToken);
            using var request = requestFactory();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessTokenAsync(cancellationToken));
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                response.Dispose();
                tokens.Invalidate();
                continue;
            }

            if (((int)response.StatusCode == 429 || (int)response.StatusCode >= 500) && attempt < 2)
            {
                var delay = RetryDelay(response, attempt);
                logger.LogWarning("Rakuten request to {Path} returned {StatusCode}; retrying after {DelayMilliseconds}ms.",
                    request.RequestUri?.AbsolutePath, (int)response.StatusCode, delay.TotalMilliseconds);
                response.Dispose();
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            return response;
        }

        throw new RakutenProviderException(RakutenFailureKind.ProviderUnavailable, "RAKUTEN_RETRY_LIMIT_EXHAUSTED");
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            return delta > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delta;
        return TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt));
    }
}
