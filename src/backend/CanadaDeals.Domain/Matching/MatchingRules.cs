using CanadaDeals.Domain.Common;

namespace CanadaDeals.Domain.Matching;

public static class MatchingRules
{
    public static MatchState Determine(
        string? trustedIdentifier,
        string? brand,
        string? modelNumber,
        IReadOnlyDictionary<string, string>? variantAttributes,
        bool titleOnlyCandidate)
    {
        if (!string.IsNullOrWhiteSpace(trustedIdentifier)) return MatchState.AutoMatched;
        if (!string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(modelNumber)) return MatchState.Confirmed;
        if (variantAttributes is { Count: > 0 }) return MatchState.PossibleMatchReview;
        return titleOnlyCandidate ? MatchState.PossibleMatchReview : MatchState.NoMatch;
    }
}
