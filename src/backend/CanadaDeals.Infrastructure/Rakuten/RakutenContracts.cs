using CanadaDeals.Domain.Common;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed record RakutenAdvertiserRecord(
    string Mid,
    string Name,
    string? Url,
    bool CanPartner,
    IReadOnlyList<string> ShipsTo,
    bool ProductFeedAvailable,
    bool DeepLinksAvailable);

public sealed record RakutenPartnershipRecord(
    string AdvertiserMid,
    string AdvertiserName,
    IntegrationAdvertiserStatus AdvertiserStatus,
    IntegrationPartnershipStatus PartnershipStatus,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? StatusUpdatedAt);

public sealed record RakutenCapabilityRecord(
    RakutenAdvertiserRecord Advertiser,
    RakutenPartnershipRecord Partnership,
    bool CanadaRelevant);

public sealed record RakutenDiscoveryResult(
    IReadOnlyList<RakutenCapabilityRecord> Capabilities,
    int AdvertisersReturned,
    int PartnershipsReturned,
    int ActivePartnerships,
    int CanadaRelevantCandidates,
    int ProductFeedCandidates,
    int DeepLinkCandidates);

public sealed record RakutenProductRecord(
    string AdvertiserMid,
    string AdvertiserName,
    string? LinkId,
    DateTimeOffset? CreatedOn,
    string? Sku,
    string ProductName,
    string? PrimaryCategory,
    string? SecondaryCategory,
    decimal? RetailPrice,
    string? RetailCurrency,
    decimal? SalePrice,
    string? SaleCurrency,
    string? Upc,
    string? ShortDescription,
    string? LongDescription,
    string? Keywords,
    string? LinkUrl,
    string? ImageUrl)
{
    public string? SourceListingKey => !string.IsNullOrWhiteSpace(LinkId) ? $"link:{LinkId.Trim()}" :
        !string.IsNullOrWhiteSpace(Sku) ? $"sku:{Sku.Trim()}" : null;

    public (decimal? Amount, string? Currency) CurrentPrice()
    {
        if (SalePrice is > 0 && !string.IsNullOrWhiteSpace(SaleCurrency) &&
            (RetailPrice is null or <= 0 || SalePrice <= RetailPrice))
            return (SalePrice, SaleCurrency.ToUpperInvariant());
        if (RetailPrice is > 0 && !string.IsNullOrWhiteSpace(RetailCurrency))
            return (RetailPrice, RetailCurrency.ToUpperInvariant());
        return (null, null);
    }
}

public sealed record RakutenProductPage(
    int TotalMatches,
    int TotalPages,
    int PageNumber,
    IReadOnlyList<RakutenProductRecord> Products);

public sealed record RakutenDeepLinkResult(
    string AdvertiserMid,
    string TrackingUrl,
    string DestinationUrl,
    string? OpaqueAttribution);

public interface IRakutenAdvertiserClient
{
    Task<IReadOnlyList<RakutenAdvertiserRecord>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IRakutenPartnershipClient
{
    Task<IReadOnlyList<RakutenPartnershipRecord>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IRakutenProductSearchClient
{
    Task<RakutenProductPage> GetPageAsync(string advertiserMid, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

public interface IRakutenDeepLinkClient
{
    Task<RakutenDeepLinkResult> CreateAsync(string advertiserMid, string destinationUrl, string? opaqueAttribution, CancellationToken cancellationToken = default);
}
