using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public interface IRakutenAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}

public sealed class RakutenAccessTokenProvider(
    HttpClient httpClient,
    IOptions<RakutenOptions> options,
    TimeProvider clock) : IRakutenAccessTokenProvider
{
    private readonly RakutenOptions _options = options.Value;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private CachedToken? _cached;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var now = clock.GetUtcNow();
        if (IsUsable(_cached, now)) return _cached!.AccessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            now = clock.GetUtcNow();
            if (IsUsable(_cached, now)) return _cached!.AccessToken;

            using var request = new HttpRequestMessage(HttpMethod.Post, "token");
            var tokenKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
            var form = new Dictionary<string, string> { ["scope"] = _options.AccountId! };
            if (!string.IsNullOrWhiteSpace(_cached?.RefreshToken)) form["refresh_token"] = _cached.RefreshToken;
            request.Content = new FormUrlEncodedContent(form);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new RakutenProviderException(RakutenFailureKind.Authentication, "RAKUTEN_TOKEN_AUTHENTICATION_FAILED");
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new RakutenProviderException(RakutenFailureKind.Authorization, "RAKUTEN_TOKEN_AUTHORIZATION_FAILED");
            if ((int)response.StatusCode == 429)
                throw new RakutenProviderException(RakutenFailureKind.RateLimited, "RAKUTEN_TOKEN_RATE_LIMITED", ReadRetryAfter(response));
            if ((int)response.StatusCode >= 500)
                throw new RakutenProviderException(RakutenFailureKind.ProviderUnavailable, "RAKUTEN_TOKEN_PROVIDER_UNAVAILABLE");
            if (!response.IsSuccessStatusCode)
                throw new RakutenProviderException(RakutenFailureKind.Authentication, "RAKUTEN_TOKEN_REQUEST_REJECTED");

            TokenPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<TokenPayload>(await response.Content.ReadAsStringAsync(cancellationToken), JsonOptions);
            }
            catch (JsonException)
            {
                throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_TOKEN_RESPONSE_MALFORMED");
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken) || payload.ExpiresIn <= 0 ||
                !string.Equals(payload.TokenType, "bearer", StringComparison.OrdinalIgnoreCase))
                throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_TOKEN_RESPONSE_INCOMPLETE");

            _cached = new CachedToken(payload.AccessToken, payload.RefreshToken, now.AddSeconds(payload.ExpiresIn));
            return _cached.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Invalidate() => _cached = null;

    private bool IsUsable(CachedToken? token, DateTimeOffset now) =>
        token is not null && token.ExpiresAt > now.AddSeconds(_options.RefreshSkewSeconds);

    private void EnsureConfigured()
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.AccountId) ||
            string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new RakutenProviderException(RakutenFailureKind.ConfigurationError, "RAKUTEN_CONFIGURATION_INCOMPLETE");
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta) return delta;
        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }
        return null;
    }

    private sealed record CachedToken(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);

    private sealed class TokenPayload
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }
        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
