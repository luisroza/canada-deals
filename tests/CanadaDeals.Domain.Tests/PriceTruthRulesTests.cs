using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.PriceTruth;

namespace CanadaDeals.Domain.Tests;

public sealed class PriceTruthRulesTests
{
    [Fact]
    public void Missing_observation_is_unknown_freshness()
    {
        var result = FreshnessCalculator.Calculate(null, DateTimeOffset.UtcNow);
        Assert.Equal(FreshnessState.Unknown, result);
    }

    [Fact]
    public void Old_observation_is_stale()
    {
        var now = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var result = FreshnessCalculator.Calculate(now.AddDays(-3), now);
        Assert.Equal(FreshnessState.Stale, result);
    }

    [Fact]
    public void Unknown_policy_prevents_strong_evidence()
    {
        var policy = MerchantPolicy.Create("unknown", PolicyPermission.Unknown, PolicyPermission.Unknown, PolicyPermission.Unknown, PolicyPermission.Unknown, null, "UNKNOWN", "UNKNOWN", "", null, "UNKNOWN", DateTimeOffset.UtcNow);
        Assert.Equal(EvidenceState.Unavailable, EvidenceCalculator.Calculate(policy, HistoryAvailability.Reliable, 10m));
    }

    [Fact]
    public void Partial_history_is_not_presented_as_reliable()
    {
        var policy = MerchantPolicy.Create("demo", PolicyPermission.Allowed, PolicyPermission.Allowed, PolicyPermission.Denied, PolicyPermission.Allowed, 24, "SAME_PRODUCT_ONLY", "DEMO", "", 7, "Local", DateTimeOffset.UtcNow);
        Assert.Equal(EvidenceState.Partial, EvidenceCalculator.Calculate(policy, HistoryAvailability.Partial, 10m));
    }
}
