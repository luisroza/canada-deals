using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class ImpactCatalogOptions
{
    public const string SectionName = "CatalogProviders:Impact";
    public bool Enabled { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class ImpactCatalogSource(
    HttpClient client,
    IOptions<AffiliateOptions> affiliateOptions,
    IOptions<ImpactCatalogOptions> catalogOptions,
    TimeProvider clock) : IOfferCatalogSource
{
    public string Provider => CatalogProviderNames.Impact;

    public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogCapabilities(true, true, false, true, false, false, 1000, "impact-publisher-catalog-v1"));

    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var config = Configuration();
        var path = $"/Mediapartners/{Uri.EscapeDataString(config.AccountSid!)}/Catalogs?PageSize={Math.Clamp(request.MaximumCandidates, 1, 1000)}";
        using var response = await SendAsync(path, config, cancellationToken);
        using var json = await CatalogHttp.ReadJsonAsync(response, "IMPACT", cancellationToken);
        var rows = Array(json.RootElement, "Catalogs");
        return rows.Take(request.MaximumCandidates).Select(row =>
        {
            var serviceAreas = Strings(row, "ServiceAreas");
            var currency = Text(row, "Currency")?.ToUpperInvariant();
            var canada = serviceAreas.Any(area => string.Equals(area, "Canada", StringComparison.OrdinalIgnoreCase));
            var catalogId = Text(row, "Id") ?? string.Empty;
            var advertiserId = Text(row, "AdvertiserId") ?? Text(row, "CampaignId") ?? string.Empty;
            var metadata = new Dictionary<string, string>();
            Add(metadata, "campaignId", Text(row, "CampaignId"));
            Add(metadata, "campaignName", Text(row, "CampaignName"));
            Add(metadata, "serviceAreas", string.Join(',', serviceAreas.Take(8)));
            return new CatalogCandidate(Provider, advertiserId, catalogId,
                Text(row, "AdvertiserName") ?? Text(row, "Name") ?? advertiserId,
                IntegrationPartnershipStatus.Active, catalogId.Length > 0, false, canada, currency,
                ParseDate(Text(row, "DateLastUpdated")), metadata);
        }).Where(candidate => candidate.ProviderAdvertiserId.Length > 0 && !string.IsNullOrWhiteSpace(candidate.CatalogId)).ToArray();
    }

    public async Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        var config = Configuration();
        if (string.IsNullOrWhiteSpace(request.CatalogId))
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, "IMPACT_CATALOG_ID_REQUIRED");
        var pageSize = Math.Clamp(request.PageSize, 1, 1000);
        var path = $"/Mediapartners/{Uri.EscapeDataString(config.AccountSid!)}/Catalogs/{Uri.EscapeDataString(request.CatalogId)}/Items?Page={Math.Max(1, request.PageNumber)}&PageSize={pageSize}";
        using var response = await SendAsync(path, config, cancellationToken);
        using var json = await CatalogHttp.ReadJsonAsync(response, "IMPACT", cancellationToken);
        var root = json.RootElement;
        var rows = Array(root, "CatalogItems");
        if (rows.Count == 0) rows = Array(root, "Items");
        if (rows.Count == 0 && root.ValueKind == JsonValueKind.Object && Text(root, "CatalogItemId") is not null) rows = [root];
        var offers = rows.Take(request.MaximumRecords).Select(row => Map(row, request.ProviderAdvertiserId, clock.GetUtcNow())).Where(row => row is not null).Cast<ExternalOffer>().ToArray();
        var pages = AttributeInt(root, "@numpages");
        var page = AttributeInt(root, "@page");
        var total = AttributeInt(root, "@total");
        return new CatalogPage(request.PageNumber, total > 0 ? total : offers.Length, offers,
            pages > 0 ? page < pages : offers.Length == pageSize);
    }

    private (string? AccountSid, string? AuthToken) Configuration()
    {
        var affiliate = affiliateOptions.Value.Impact;
        if (!catalogOptions.Value.Enabled || !affiliate.Enabled || string.IsNullOrWhiteSpace(affiliate.AccountSid) || string.IsNullOrWhiteSpace(affiliate.AuthToken))
            throw new CatalogProviderException(CatalogFailureKind.Configuration, "IMPACT_CATALOG_CONFIGURATION_DISABLED");
        if (!Uri.TryCreate(affiliate.BaseUrl, UriKind.Absolute, out var origin) ||
            !string.Equals(origin.IdnHost, "api.impact.com", StringComparison.OrdinalIgnoreCase))
            throw new CatalogProviderException(CatalogFailureKind.Configuration, "IMPACT_PROVIDER_ORIGIN_REJECTED");
        return (affiliate.AccountSid, affiliate.AuthToken);
    }

    private Task<HttpResponseMessage> SendAsync(string path, (string? AccountSid, string? AuthToken) config, CancellationToken cancellationToken) =>
        CatalogHttp.SendAsync(client, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.AccountSid}:{config.AuthToken}")));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return request;
        }, "IMPACT", cancellationToken);

    private static ExternalOffer? Map(JsonElement row, string expectedAdvertiserId, DateTimeOffset fetchedAt)
    {
        var advertiserId = Text(row, "AdvertiserId") ?? Text(row, "CampaignId") ?? expectedAdvertiserId;
        if (!string.Equals(advertiserId, expectedAdvertiserId, StringComparison.Ordinal)) return null;
        var id = Text(row, "CatalogItemId") ?? Text(row, "Id");
        var title = Text(row, "Name");
        var url = Text(row, "Url");
        if (id is null || title is null || url is null) return null;
        var current = Decimal(Text(row, "CurrentPrice"));
        var regular = Decimal(Text(row, "OriginalPrice"));
        var availability = Text(row, "StockAvailability")?.ToLowerInvariant() switch
        {
            "instock" or "limitedavailability" => OnlineAvailabilityState.Available,
            "outofstock" => OnlineAvailabilityState.Unavailable,
            _ => OnlineAvailabilityState.Unknown
        };
        var condition = Text(row, "Condition")?.ToLowerInvariant() switch
        {
            "new" => ProductCondition.New,
            "used" => ProductCondition.Used,
            var value when value?.Contains("refurb", StringComparison.Ordinal) == true => ProductCondition.Refurbished,
            _ => ProductCondition.Unknown
        };
        var metadata = new Dictionary<string, string>();
        Add(metadata, "catalogId", Text(row, "CatalogId"));
        Add(metadata, "campaignId", Text(row, "CampaignId"));
        Add(metadata, "stockAvailability", Text(row, "StockAvailability"));
        Add(metadata, "itemGroupId", Text(row, "ItemGroupId"));
        return new ExternalOffer(CatalogProviderNames.Impact, advertiserId, null, id, title, title,
            Text(row, "Manufacturer"), Text(row, "Sku"), null, Text(row, "Gtin"), Text(row, "Mpn"), null,
            current, regular, Text(row, "Currency")?.ToUpperInvariant(), null, ParseDate(Text(row, "ExpirationDate")),
            url, null, Text(row, "ImageUrl"), condition, null, null, availability, null,
            Shipping(row), Text(row, "Category"), Text(row, "SubCategory"), ParseDate(Text(row, "LaunchDate")),
            null, fetchedAt, metadata);
    }

    private static string? Shipping(JsonElement row)
    {
        var rate = Text(row, "ShippingRate");
        var label = Text(row, "ShippingLabel");
        return string.Join(' ', new[] { rate, label }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static IReadOnlyList<JsonElement> Array(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var rows) && rows.ValueKind == JsonValueKind.Array ? rows.EnumerateArray().ToArray() : [];
    private static IReadOnlyList<string> Strings(JsonElement root, string property) =>
        root.TryGetProperty(property, out var rows) && rows.ValueKind == JsonValueKind.Array ? rows.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!).ToArray() : [];
    private static string? Text(JsonElement value, string property) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var item) && item.ValueKind is JsonValueKind.String or JsonValueKind.Number ? item.ToString() : null;
    private static int AttributeInt(JsonElement value, string property) => int.TryParse(Text(value, property), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    private static decimal? Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim(); }
}
