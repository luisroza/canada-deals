namespace CanadaDeals.Api.Contracts;

public sealed record DealCardResponse(
    Guid ListingId,
    string ProductSlug,
    string ProductTitle,
    string Brand,
    string Category,
    string Retailer,
    decimal? CurrentPrice,
    string Currency,
    string FreshnessState,
    string EvidenceState,
    string EvidenceExplanation,
    DateTimeOffset? ObservedAt,
    string MatchState,
    string HistoryState,
    bool HasSafeComparison,
    string DetailsPath,
    string HandoffPath,
    string Disclosure);

public sealed record RetailerOfferResponse(
    Guid ListingId,
    string Retailer,
    string Title,
    decimal? CurrentPrice,
    string Currency,
    string FreshnessState,
    string EvidenceState,
    string MatchState,
    string HistoryState,
    DateTimeOffset? ObservedAt,
    string HandoffPath,
    string Disclosure,
    bool IsSafeComparison);

public sealed record ProductDetailResponse(
    string ProductSlug,
    string ProductTitle,
    string Brand,
    string Category,
    IReadOnlyDictionary<string, string> VariantAttributes,
    RetailerOfferResponse PrimaryOffer,
    IReadOnlyList<RetailerOfferResponse> SafeComparisons,
    IReadOnlyList<RetailerOfferResponse> RelatedListingsForReview,
    string HistorySummary,
    string EvidenceSummary);

public sealed record DiscoveryResponse(IReadOnlyList<DealCardResponse> Items, int Count, string Sort);
