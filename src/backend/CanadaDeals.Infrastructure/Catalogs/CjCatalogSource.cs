using System.Globalization;
using System.Net.Http.Headers;
using System.Xml.Linq;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class CjCatalogOptions
{
    public const string SectionName = "CatalogProviders:Cj";
    public bool Enabled { get; init; }
    public string ProductSearchBaseUrl { get; init; } = "https://product-search.api.cj.com";
    public string? WebsiteId { get; init; }
    public string[] CandidateAdvertiserIds { get; init; } = [];
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class CjCatalogSource(
    HttpClient client,
    IOptions<AffiliateOptions> affiliateOptions,
    IOptions<CjCatalogOptions> catalogOptions,
    TimeProvider clock) : IOfferCatalogSource
{
    public string Provider => CatalogProviderNames.Cj;

    public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogCapabilities(true, true, false, true, true, false, 1000, "cj-product-search-v2"));

    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        Configuration();
        var result = new List<CatalogCandidate>();
        foreach (var advertiserId in catalogOptions.Value.CandidateAdvertiserIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(request.MaximumCandidates))
        {
            var page = await FetchOffersAsync(new CatalogRequest(advertiserId.Trim(), Query: "*", PageSize: 1, MaximumRecords: 1), cancellationToken);
            if (page.Offers.Count == 0) continue;
            var first = page.Offers[0];
            result.Add(new CatalogCandidate(Provider, advertiserId.Trim(), null,
                first.ProviderMetadata.GetValueOrDefault("advertiserName") ?? advertiserId.Trim(),
                IntegrationPartnershipStatus.Active, true, true, null, first.Currency,
                first.SourceUpdatedAt, new Dictionary<string, string> { ["access"] = "product-search" }));
        }
        return result;
    }

    public async Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        var config = Configuration();
        if (string.IsNullOrWhiteSpace(request.ProviderAdvertiserId))
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, "CJ_ADVERTISER_ID_REQUIRED");
        var pageSize = Math.Clamp(request.PageSize, 1, 1000);
        var query = new Dictionary<string, string>
        {
            ["website-id"] = config.WebsiteId!,
            ["advertiser-ids"] = request.ProviderAdvertiserId,
            ["page-number"] = Math.Max(1, request.PageNumber).ToString(CultureInfo.InvariantCulture),
            ["records-per-page"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(request.Query) && request.Query != "*") query["keywords"] = request.Query.Trim();
        var path = "/v2/product-search?" + string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        using var response = await CatalogHttp.SendAsync(client, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, path);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            return message;
        }, "CJ", cancellationToken);
        var document = await CatalogHttp.ReadXmlAsync(response, "CJ", cancellationToken);
        var container = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "products");
        if (container is null) throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "CJ_PRODUCTS_CONTAINER_MISSING");
        var offers = container.Elements().Where(element => element.Name.LocalName == "product")
            .Take(request.MaximumRecords).Select(element => Map(element, request.ProviderAdvertiserId, clock.GetUtcNow()))
            .Where(offer => offer is not null).Cast<ExternalOffer>().ToArray();
        var total = IntAttribute(container, "total-matched");
        var page = IntAttribute(container, "page-number");
        return new CatalogPage(request.PageNumber, total, offers, page * pageSize < total);
    }

    private (string? WebsiteId, string? Token) Configuration()
    {
        var affiliate = affiliateOptions.Value.Cj;
        var catalog = catalogOptions.Value;
        if (!catalog.Enabled || !affiliate.Enabled || string.IsNullOrWhiteSpace(affiliate.PersonalAccessToken) || string.IsNullOrWhiteSpace(catalog.WebsiteId))
            throw new CatalogProviderException(CatalogFailureKind.Configuration, "CJ_CATALOG_CONFIGURATION_DISABLED");
        return (catalog.WebsiteId, affiliate.PersonalAccessToken);
    }

    private static ExternalOffer? Map(XElement row, string expectedAdvertiserId, DateTimeOffset fetchedAt)
    {
        var advertiserId = Text(row, "advertiser-id") ?? expectedAdvertiserId;
        if (!string.Equals(advertiserId, expectedAdvertiserId, StringComparison.Ordinal)) return null;
        var id = Text(row, "sku") ?? Text(row, "catalog-id") ?? Text(row, "upc");
        var title = Text(row, "name");
        var url = Text(row, "buy-url");
        if (id is null || title is null || url is null) return null;
        var price = Decimal(Text(row, "price"));
        var sale = Decimal(Text(row, "sale-price"));
        var current = sale is > 0 && (price is null || sale < price) ? sale : price;
        var regular = sale is > 0 && price > sale ? price : null;
        var metadata = new Dictionary<string, string>();
        Add(metadata, "advertiserName", Text(row, "advertiser-name"));
        Add(metadata, "catalogId", Text(row, "catalog-id"));
        return new ExternalOffer(CatalogProviderNames.Cj, advertiserId, null, id, title, title,
            Text(row, "manufacturer-name"), Text(row, "sku"), Text(row, "upc"), Text(row, "gtin"),
            Text(row, "manufacturer-sku"), null, current, regular, Text(row, "currency")?.ToUpperInvariant(),
            null, null, url, url, Text(row, "image-url"), ProductCondition.Unknown, null, null,
            Availability(Text(row, "in-stock")), null, null, Text(row, "advertiser-category"), null,
            null, ParseDate(Text(row, "last-updated")), fetchedAt, metadata);
    }

    private static OnlineAvailabilityState Availability(string? value) => value?.ToLowerInvariant() switch
    {
        "true" or "yes" or "1" => OnlineAvailabilityState.Available,
        "false" or "no" or "0" => OnlineAvailabilityState.Unavailable,
        _ => OnlineAvailabilityState.Unknown
    };
    private static string? Text(XElement parent, string localName) => parent.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value?.Trim() is { Length: > 0 } value ? value : null;
    private static int IntAttribute(XElement value, string name) => int.TryParse(value.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    private static decimal? Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim(); }
}
