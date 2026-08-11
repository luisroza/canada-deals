using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.PriceTruth;

public sealed record FreshnessThresholds(TimeSpan RecentWindow, TimeSpan AgingWindow)
{
    public static FreshnessThresholds Default { get; } = new(TimeSpan.FromHours(6), TimeSpan.FromHours(24));
}

public static class FreshnessCalculator
{
    public static FreshnessState Calculate(DateTimeOffset? observedAt, DateTimeOffset now, FreshnessThresholds? thresholds = null)
    {
        if (observedAt is null) return FreshnessState.Unknown;
        var age = now - observedAt.Value;
        var policy = thresholds ?? FreshnessThresholds.Default;
        if (age < TimeSpan.Zero) age = TimeSpan.Zero;
        if (age <= policy.RecentWindow) return FreshnessState.Recent;
        if (age <= policy.AgingWindow) return FreshnessState.Aging;
        return FreshnessState.Stale;
    }
}

public static class EvidenceCalculator
{
    public static EvidenceState Calculate(MerchantPolicy policy, HistoryAvailability history, decimal? currentPrice)
    {
        if (!policy.CanPublishCurrentPrice || currentPrice is null) return EvidenceState.Unavailable;
        if (history == HistoryAvailability.Reliable) return EvidenceState.Strong;
        if (history == HistoryAvailability.Partial) return EvidenceState.Partial;
        return EvidenceState.Unknown;
    }
}

public static class ComparisonRules
{
    public static bool IsSafeComparison(RetailerListing listing) =>
        listing.MatchState is MatchState.AutoMatched or MatchState.Confirmed;
}
