using CanadaDeals.Domain.Common;

namespace CanadaDeals.Infrastructure.Catalogs;

public static class CatalogProviderNames
{
    public const string Rakuten = "rakuten";
    public const string Ebay = "ebay";
    public const string Impact = "impact";
    public const string Awin = "awin";
    public const string Cj = "cj";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Rakuten, Ebay, Impact, Awin, Cj
    };
}

public sealed record CatalogCapabilities(
    bool Discovery,
    bool KeywordSearch,
    bool CategorySearch,
    bool Pagination,
    bool AffiliateUrls,
    bool StreamingFeed,
    int MaximumPageSize,
    string ContractVersion);

public sealed record CatalogDiscoveryRequest(int MaximumCandidates = 100);

public sealed record CatalogCandidate(
    string Provider,
    string ProviderAdvertiserId,
    string? CatalogId,
    string DisplayName,
    IntegrationPartnershipStatus RelationshipStatus,
    bool CatalogAvailable,
    bool AffiliateAvailable,
    bool? CanadaRelevant,
    string? Currency,
    DateTimeOffset? SourceUpdatedAt,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record CatalogRequest(
    string ProviderAdvertiserId,
    string? CatalogId = null,
    string? Query = null,
    string? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 50,
    int MaximumRecords = 100,
    string? Cursor = null);

public sealed record ExternalOffer(
    string Provider,
    string ProviderAdvertiserId,
    string? RetailerKey,
    string ExternalListingId,
    string Title,
    string OriginalTitle,
    string? Brand,
    string? Sku,
    string? Upc,
    string? Gtin,
    string? Mpn,
    string? Model,
    decimal? CurrentPrice,
    decimal? RegularPrice,
    string? Currency,
    DateTimeOffset? PromotionStart,
    DateTimeOffset? PromotionEnd,
    string DestinationUrl,
    string? ProviderAffiliateUrl,
    string? ImageUrl,
    ProductCondition Condition,
    string? Seller,
    bool? Marketplace,
    OnlineAvailabilityState Availability,
    string? Region,
    string? Shipping,
    string? PrimaryCategory,
    string? SecondaryCategory,
    DateTimeOffset? SourceCreatedAt,
    DateTimeOffset? SourceUpdatedAt,
    DateTimeOffset FetchedAt,
    IReadOnlyDictionary<string, string> ProviderMetadata)
{
    public string SourceListingKey => ExternalListingId.Trim();
}

public sealed record CatalogPage(
    int PageNumber,
    int RecordsAvailable,
    IReadOnlyList<ExternalOffer> Offers,
    bool HasMore,
    string? NextCursor = null);

public interface IOfferCatalogSource
{
    string Provider { get; }
    Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default);
    Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default);
}

public enum CatalogFailureKind
{
    Configuration,
    Authentication,
    Authorization,
    RelationshipDenied,
    RateLimited,
    ProviderUnavailable,
    MalformedResponse,
    PayloadTooLarge,
    InvalidRequest
}

public sealed class CatalogProviderException(
    CatalogFailureKind kind,
    string safeCode,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : Exception(safeCode, innerException)
{
    public CatalogFailureKind Kind { get; } = kind;
    public string SafeCode { get; } = safeCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

public sealed class CatalogIngestionOptions
{
    public const string SectionName = "CatalogIngestion";
    public bool PersistenceEnabled { get; init; }
    public int MaximumPagesPerRun { get; init; } = 2;
    public int PageSize { get; init; } = 50;
    public int MaximumRecordsPerRun { get; init; } = 100;
    public int MaximumMetadataEntries { get; init; } = 16;
    public int MaximumMetadataValueLength { get; init; } = 240;
}

public sealed record CatalogImportSummary(
    Guid RunId,
    string Provider,
    string ProviderAdvertiserId,
    IntegrationRunStatus Status,
    bool DryRun,
    int Pages,
    int Records,
    int Valid,
    int Cad,
    int Mapped,
    int Unmapped,
    int Created,
    int Updated,
    int Observations,
    int Skipped,
    int PolicyBlocked,
    int ReviewCandidates,
    int UnsupportedCurrency,
    int Invalid,
    string? FailureReason);
