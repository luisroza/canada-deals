using System.ComponentModel.DataAnnotations;

namespace CanadaDeals.Api.Contracts;

public sealed class DiscoveryQueryRequest : IValidatableObject
{
    [MaxLength(120)] public string? Search { get; init; }
    [MaxLength(300)] public string? Category { get; init; }
    [MaxLength(300)] public string? Retailer { get; init; }
    [Range(typeof(decimal), "0", "1000000")] public decimal? MinPrice { get; init; }
    [Range(typeof(decimal), "0", "1000000")] public decimal? MaxPrice { get; init; }
    public bool? HasReference { get; init; }
    [MaxLength(100)] public string? Freshness { get; init; }
    [MaxLength(100)] public string? Match { get; init; }
    [MaxLength(100)] public string? Availability { get; init; }
    [MaxLength(30)] public string? Sort { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 48)] public int PageSize { get; init; } = 24;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            yield return new ValidationResult("Minimum price cannot be greater than maximum price.", [nameof(MinPrice), nameof(MaxPrice)]);

        foreach (var error in ValidateValues(Sort, ["relevance", "recent", "savings", "price-asc"], nameof(Sort), single: true)) yield return error;
        foreach (var error in ValidateValues(Freshness, ["recent", "aging", "stale", "unknown"], nameof(Freshness))) yield return error;
        foreach (var error in ValidateValues(Match, ["safe", "review", "none"], nameof(Match))) yield return error;
        foreach (var error in ValidateValues(Availability, ["online", "unavailable", "unknown"], nameof(Availability))) yield return error;
        foreach (var error in ValidateSlugValues(Category, nameof(Category))) yield return error;
        foreach (var error in ValidateSlugValues(Retailer, nameof(Retailer))) yield return error;
    }

    public static string[] Values(string? raw) => string.IsNullOrWhiteSpace(raw)
        ? []
        : raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IEnumerable<ValidationResult> ValidateValues(string? raw, string[] allowed, string member, bool single = false)
    {
        var values = Values(raw);
        if (single && values.Length > 1)
            yield return new ValidationResult($"{member} accepts one value.", [member]);
        if (values.Any(value => !allowed.Contains(value, StringComparer.OrdinalIgnoreCase)))
            yield return new ValidationResult($"Unsupported {member.ToLowerInvariant()} value.", [member]);
    }

    private static IEnumerable<ValidationResult> ValidateSlugValues(string? raw, string member)
    {
        var values = Values(raw);
        if (values.Length > 10)
            yield return new ValidationResult($"{member} accepts at most 10 values.", [member]);
        if (values.Any(value => value.Length > 80 || value.Any(character => !char.IsLetterOrDigit(character) && character != '-')))
            yield return new ValidationResult($"Invalid {member.ToLowerInvariant()} key.", [member]);
    }
}

public sealed record DealCardResponse(
    Guid ListingId,
    Guid ProductId,
    string ProductSlug,
    string ProductTitle,
    string Brand,
    string Category,
    string Retailer,
    decimal? CurrentPrice,
    string Currency,
    string FreshnessState,
    string EvidenceState,
    string AvailabilityState,
    string EvidenceExplanation,
    DateTimeOffset? ObservedAt,
    string MatchState,
    string HistoryState,
    decimal? ReferencePrice,
    decimal? SupportedSavingsPercent,
    bool HasSafeComparison,
    string DetailsPath,
    string? HandoffPath,
    string? HandoffUrl,
    string HandoffMode,
    string Disclosure,
    ProductImageResponse? ProductImage);

public sealed record ProductImageResponse(
    string Url,
    int Width,
    int Height);

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
    string AvailabilityState,
    string? Seller,
    string ConditionState,
    string? RegionAvailabilityContext,
    string? ShippingContext,
    DateTimeOffset? ObservedAt,
    string? HandoffPath,
    string? HandoffUrl,
    string HandoffMode,
    string Disclosure,
    bool IsSafeComparison);

public sealed record ProductDetailResponse(
    Guid ProductId,
    string ProductSlug,
    string ProductTitle,
    string Brand,
    string Category,
    IReadOnlyDictionary<string, string> VariantAttributes,
    RetailerOfferResponse PrimaryOffer,
    IReadOnlyList<RetailerOfferResponse> SafeComparisons,
    IReadOnlyList<RetailerOfferResponse> RelatedListingsForReview,
    string HistorySummary,
    string EvidenceSummary,
    ProductImageResponse? ProductImage);

public sealed record ProductHistoryPointResponse(
    DateTimeOffset ObservedDate,
    decimal LowestPrice,
    string Currency,
    int ObservationCount);

public sealed record ProductHistoryResponse(
    Guid ProductId,
    string ProductSlug,
    string Window,
    int WindowDays,
    string State,
    DateTimeOffset? TrackingStart,
    DateTimeOffset? ObservationStart,
    DateTimeOffset? ObservationEnd,
    decimal? LowestObservedPrice,
    decimal? HighestObservedPrice,
    int ObservationCount,
    int ObservedDayCount,
    int? LargestGapDays,
    string CoverageSummary,
    string Interpretation,
    IReadOnlyList<ProductHistoryPointResponse> Points);

public sealed record DiscoveryFacetOption(string Key, string Label);

public sealed record StoreBannerResponse(
    string RetailerKey,
    string DisplayName,
    string Title,
    string Subtitle,
    string AssetPath,
    string AssetSource,
    string BrandAssetPolicy,
    string AffiliateStatus,
    string Href,
    bool OpensNewTab);

public sealed record DiscoveryFacetsResponse(
    IReadOnlyList<DiscoveryFacetOption> Categories,
    IReadOnlyList<DiscoveryFacetOption> Retailers);

public sealed record DiscoveryResponse(
    IReadOnlyList<DealCardResponse> Items,
    int Count,
    string Sort,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    DiscoveryFacetsResponse Facets);

public sealed record SavedProductResponse(
    Guid ProductId,
    string ProductSlug,
    string ProductTitle,
    string Brand,
    string Category,
    decimal? CurrentPrice,
    string Currency,
    string FreshnessState,
    string EvidenceState,
    string HistoryState,
    string? Retailer,
    DateTimeOffset SavedAt,
    string DetailsPath,
    ProductImageResponse? ProductImage);
