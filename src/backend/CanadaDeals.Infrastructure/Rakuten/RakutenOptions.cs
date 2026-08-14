using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenOptions
{
    public const string SectionName = "Rakuten";
    public bool Enabled { get; init; }
    public bool LiveDiscoveryEnabled { get; init; }
    public bool CatalogImportEnabled { get; init; }
    public bool DeepLinkEnabled { get; init; }
    public string ApiBaseUrl { get; init; } = "https://api.linksynergy.com";
    public string? AccountId { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public int RefreshSkewSeconds { get; init; } = 120;
    public int MinimumRequestIntervalMilliseconds { get; init; } = 750;
    public int ProductPageSize { get; init; } = 20;
    public int MaximumPagesPerRun { get; init; } = 2;
}

public sealed class RakutenOptionsValidator : IValidateOptions<RakutenOptions>
{
    public ValidateOptionsResult Validate(string? name, RakutenOptions options)
    {
        var failures = new List<string>();
        if (!options.Enabled) return ValidateOptionsResult.Success;

        if (!ValidHttpsOrigin(options.ApiBaseUrl)) failures.Add("Rakuten:ApiBaseUrl must be an HTTPS origin without credentials, path, query, or fragment.");
        if (string.IsNullOrWhiteSpace(options.AccountId)) failures.Add("Rakuten:AccountId is required when Rakuten is enabled.");
        if (string.IsNullOrWhiteSpace(options.ClientId)) failures.Add("Rakuten:ClientId is required when Rakuten is enabled.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret)) failures.Add("Rakuten:ClientSecret is required when Rakuten is enabled.");
        if (options.TimeoutSeconds is < 5 or > 120) failures.Add("Rakuten:TimeoutSeconds must be between 5 and 120.");
        if (options.RefreshSkewSeconds is < 30 or > 900) failures.Add("Rakuten:RefreshSkewSeconds must be between 30 and 900.");
        if (options.MinimumRequestIntervalMilliseconds is < 600 or > 10000)
            failures.Add("Rakuten:MinimumRequestIntervalMilliseconds must be between 600 and 10000 to stay below the documented 100 requests/minute boundary.");
        if (options.ProductPageSize is < 1 or > 100) failures.Add("Rakuten:ProductPageSize must be between 1 and 100.");
        if (options.MaximumPagesPerRun is < 1 or > 20) failures.Add("Rakuten:MaximumPagesPerRun must be between 1 and 20.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool ValidHttpsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath == "/" &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

public enum RakutenFailureKind
{
    Authentication,
    Authorization,
    RateLimited,
    PartnershipDenied,
    DeepLinkDisabled,
    AdvertiserInactive,
    InvalidDestination,
    ProviderUnavailable,
    MalformedResponse,
    ConfigurationError
}

public sealed class RakutenProviderException(RakutenFailureKind kind, string safeCode, TimeSpan? retryAfter = null)
    : Exception(safeCode)
{
    public RakutenFailureKind Kind { get; } = kind;
    public string SafeCode { get; } = safeCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
