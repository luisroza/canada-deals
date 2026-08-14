namespace CanadaDeals.Infrastructure.Affiliates;

public sealed class AffiliateOptions
{
    public const string SectionName = "Affiliate";
    public ImpactAffiliateOptions Impact { get; init; } = new();
    public CjAffiliateOptions Cj { get; init; } = new();
    public int RevalidateHours { get; init; } = 168;
    public int FailureRetryMinutes { get; init; } = 60;
}

public sealed class ImpactAffiliateOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://api.impact.com";
    public string? AccountSid { get; init; }
    public string? AuthToken { get; init; }
}

public sealed class CjAffiliateOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://link-search.api.cj.com";
    public string? PersonalAccessToken { get; init; }
}
