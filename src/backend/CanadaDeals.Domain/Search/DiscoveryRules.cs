using System.Text;

namespace CanadaDeals.Domain.Search;

public enum DiscoverySort
{
    Relevance,
    RecentlyChecked,
    SupportedSavings,
    LowestPrice
}

public static class DiscoveryRules
{
    public const int DefaultPageSize = 24;
    public const int MaximumPageSize = 48;
    public const decimal MaximumPrice = 1_000_000m;
    // Matches PostgreSQL pg_trgm's default word_similarity_threshold used by the indexable <% operator.
    public const double MinimumTrigramSimilarity = 0.60d;

    public static DiscoverySort DefaultSort(string? search) =>
        string.IsNullOrWhiteSpace(search) ? DiscoverySort.RecentlyChecked : DiscoverySort.Relevance;

    public static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character)) normalized.Append(character);
        }
        return normalized.ToString();
    }

    public static string PublicMatchGroup(Common.MatchState state) => state switch
    {
        Common.MatchState.AutoMatched or Common.MatchState.Confirmed => "safe",
        Common.MatchState.PossibleMatchReview or Common.MatchState.ManualReview => "review",
        _ => "none"
    };

    public static bool SupportedSavings(decimal currentPrice, decimal? regularPrice, string? evidenceReference) =>
        regularPrice.HasValue && regularPrice.Value > currentPrice && !string.IsNullOrWhiteSpace(evidenceReference);

    public static decimal? SavingsAmount(decimal currentPrice, decimal? regularPrice, string? evidenceReference) =>
        SupportedSavings(currentPrice, regularPrice, evidenceReference) ? regularPrice!.Value - currentPrice : null;

    public static decimal? SavingsPercent(decimal currentPrice, decimal? regularPrice, string? evidenceReference) =>
        SupportedSavings(currentPrice, regularPrice, evidenceReference)
            ? Math.Round((regularPrice!.Value - currentPrice) / regularPrice.Value * 100m, 1)
            : null;

    public static string SortKey(DiscoverySort sort) => sort switch
    {
        DiscoverySort.Relevance => "relevance",
        DiscoverySort.RecentlyChecked => "recent",
        DiscoverySort.SupportedSavings => "savings",
        DiscoverySort.LowestPrice => "price-asc",
        _ => throw new ArgumentOutOfRangeException(nameof(sort))
    };
}
