using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Rakuten;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class RakutenCatalogSource(
    IRakutenProductSearchClient products,
    RakutenDiscoveryService discovery,
    TimeProvider clock) : IOfferCatalogSource
{
    public string Provider => CatalogProviderNames.Rakuten;

    public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogCapabilities(true, false, false, true, false, false, 100, "rakuten-product-search-xml"));

    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var result = await discovery.DiscoverAsync(false, cancellationToken);
        return result.Capabilities.Take(request.MaximumCandidates).Select(capability =>
            new CatalogCandidate(Provider, capability.Advertiser.Mid, null, capability.Advertiser.Name,
                capability.Partnership.PartnershipStatus, capability.Advertiser.ProductFeedAvailable,
                capability.Advertiser.DeepLinksAvailable, capability.CanadaRelevant, null,
                capability.Partnership.StatusUpdatedAt,
                new Dictionary<string, string>
                {
                    ["advertiserStatus"] = capability.Partnership.AdvertiserStatus.ToString(),
                    ["shipsTo"] = string.Join(',', capability.Advertiser.ShipsTo.Take(8))
                })).ToArray();
    }

    public async Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        var page = await products.GetPageAsync(request.ProviderAdvertiserId, Math.Max(1, request.PageNumber),
            Math.Clamp(request.PageSize, 1, 100), cancellationToken);
        var fetchedAt = clock.GetUtcNow();
        var offers = page.Products.Take(request.MaximumRecords).Select(product => Map(product, fetchedAt))
            .Where(offer => offer is not null).Cast<ExternalOffer>().ToArray();
        return new CatalogPage(page.PageNumber, page.TotalMatches, offers, page.PageNumber < page.TotalPages);
    }

    private static ExternalOffer? Map(RakutenProductRecord source, DateTimeOffset fetchedAt)
    {
        if (source.SourceListingKey is null || string.IsNullOrWhiteSpace(source.ProductName) || string.IsNullOrWhiteSpace(source.LinkUrl)) return null;
        var (current, currency) = source.CurrentPrice();
        var regular = source.SalePrice is > 0 && source.RetailPrice > source.SalePrice &&
            string.Equals(source.SaleCurrency, source.RetailCurrency, StringComparison.OrdinalIgnoreCase)
            ? source.RetailPrice : null;
        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(source.LinkId)) metadata["linkId"] = source.LinkId.Trim();
        return new ExternalOffer(CatalogProviderNames.Rakuten, source.AdvertiserMid, null,
            source.SourceListingKey, source.ProductName, source.ProductName, null, source.Sku, source.Upc,
            source.Upc, null, null, current, regular, currency, null, null, source.LinkUrl, null,
            source.ImageUrl, ProductCondition.Unknown, null, null, OnlineAvailabilityState.Unknown,
            "Canada", null, source.PrimaryCategory, source.SecondaryCategory, source.CreatedOn, null,
            fetchedAt, metadata);
    }
}
