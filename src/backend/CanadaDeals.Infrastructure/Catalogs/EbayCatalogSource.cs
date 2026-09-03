using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CanadaDeals.Domain.Common;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class EbayCatalogOptions
{
    public const string SectionName = "CatalogProviders:Ebay";
    public bool Enabled { get; init; }
    public string ApiBaseUrl { get; init; } = "https://api.ebay.com";
    public string Marketplace { get; init; } = "EBAY_CA";
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? AffiliateCampaignId { get; init; }
    public string? AffiliateReferenceId { get; init; }
    public string DefaultQuery { get; init; } = "deals";
    public int TimeoutSeconds { get; init; } = 30;
}

public interface IEbayTokenProvider
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}

public sealed class EbayTokenProvider(HttpClient client, IOptions<EbayCatalogOptions> options, TimeProvider clock) : IEbayTokenProvider
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && _expiresAt > clock.GetUtcNow().AddMinutes(2)) return _token;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _expiresAt > clock.GetUtcNow().AddMinutes(2)) return _token;
            var config = options.Value;
            EnsureEnabled(config);
            using var response = await CatalogHttp.SendAsync(client, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/identity/v1/oauth2/token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "https://api.ebay.com/oauth/api_scope"
                });
                return request;
            }, "EBAY", cancellationToken);
            using var json = await CatalogHttp.ReadJsonAsync(response, "EBAY", cancellationToken);
            if (!json.RootElement.TryGetProperty("access_token", out var token) || string.IsNullOrWhiteSpace(token.GetString()))
                throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "EBAY_TOKEN_MALFORMED");
            var seconds = json.RootElement.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var parsed) ? parsed : 7200;
            _token = token.GetString();
            _expiresAt = clock.GetUtcNow().AddSeconds(Math.Max(300, seconds));
            return _token!;
        }
        finally { _lock.Release(); }
    }

    private static void EnsureEnabled(EbayCatalogOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
            throw new CatalogProviderException(CatalogFailureKind.Configuration, "EBAY_CONFIGURATION_DISABLED");
    }
}

public sealed class EbayCatalogSource(
    HttpClient client,
    IEbayTokenProvider tokenProvider,
    IOptions<EbayCatalogOptions> options,
    TimeProvider clock) : IOfferCatalogSource
{
    public string Provider => CatalogProviderNames.Ebay;

    public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogCapabilities(true, true, true, true,
            !string.IsNullOrWhiteSpace(options.Value.AffiliateCampaignId), false, 200, "ebay-browse-v1"));

    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        var page = await FetchOffersAsync(new CatalogRequest(config.Marketplace, Query: config.DefaultQuery,
            PageSize: 1, MaximumRecords: 1), cancellationToken);
        return
        [
            new CatalogCandidate(Provider, config.Marketplace, null, "eBay Canada",
                IntegrationPartnershipStatus.Active, page.RecordsAvailable >= 0, false, true, "CAD",
                clock.GetUtcNow(), new Dictionary<string, string> { ["marketplace"] = config.Marketplace })
        ];
    }

    public async Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (!config.Enabled) throw new CatalogProviderException(CatalogFailureKind.Configuration, "EBAY_CONFIGURATION_DISABLED");
        if (!string.Equals(config.Marketplace, "EBAY_CA", StringComparison.Ordinal) ||
            !string.Equals(request.ProviderAdvertiserId, config.Marketplace, StringComparison.Ordinal))
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, "EBAY_CANADIAN_MARKETPLACE_REQUIRED");
        if (string.IsNullOrWhiteSpace(request.Query) && string.IsNullOrWhiteSpace(request.CategoryId))
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, "EBAY_QUERY_OR_CATEGORY_REQUIRED");

        var token = await tokenProvider.GetAsync(cancellationToken);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var offset = Math.Max(0, request.PageNumber - 1) * pageSize;
        var parameters = new List<string>
        {
            $"limit={pageSize}", $"offset={offset}", "filter=deliveryCountry%3ACA"
        };
        if (!string.IsNullOrWhiteSpace(request.Query)) parameters.Add($"q={Uri.EscapeDataString(request.Query.Trim())}");
        if (!string.IsNullOrWhiteSpace(request.CategoryId)) parameters.Add($"category_ids={Uri.EscapeDataString(request.CategoryId.Trim())}");
        var url = "/buy/browse/v1/item_summary/search?" + string.Join('&', parameters);

        using var response = await CatalogHttp.SendAsync(client, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            message.Headers.TryAddWithoutValidation("X-EBAY-C-MARKETPLACE-ID", config.Marketplace);
            var context = "contextualLocation=country%3DCA";
            if (!string.IsNullOrWhiteSpace(config.AffiliateCampaignId))
            {
                context += $",affiliateCampaignId={config.AffiliateCampaignId.Trim()}";
                if (!string.IsNullOrWhiteSpace(config.AffiliateReferenceId))
                    context += $",affiliateReferenceId={config.AffiliateReferenceId.Trim()}";
            }
            message.Headers.TryAddWithoutValidation("X-EBAY-C-ENDUSERCTX", context);
            return message;
        }, "EBAY", cancellationToken);
        using var json = await CatalogHttp.ReadJsonAsync(response, "EBAY", cancellationToken);
        var root = json.RootElement;
        var total = Int(root, "total");
        var offers = new List<ExternalOffer>();
        if (root.TryGetProperty("itemSummaries", out var rows) && rows.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rows.EnumerateArray().Take(request.MaximumRecords))
            {
                var mapped = Map(row, config.Marketplace, clock.GetUtcNow());
                if (mapped is not null) offers.Add(mapped);
            }
        }
        return new CatalogPage(request.PageNumber, total, offers, offset + pageSize < total);
    }

    private static ExternalOffer? Map(JsonElement row, string marketplace, DateTimeOffset fetchedAt)
    {
        var id = Text(row, "itemId");
        var title = Text(row, "title");
        var itemUrl = Text(row, "itemWebUrl");
        var price = Money(row, "price");
        if (id is null || title is null || itemUrl is null) return null;
        var original = row.TryGetProperty("marketingPrice", out var marketing) ? Money(marketing, "originalPrice") : (null, null);
        var identifiers = Identifiers(row);
        var metadata = new Dictionary<string, string>();
        Add(metadata, "buyingOptions", JoinArray(row, "buyingOptions"));
        Add(metadata, "itemLocationCountry", NestedText(row, "itemLocation", "country"));
        return new ExternalOffer(CatalogProviderNames.Ebay, marketplace, "ebay-ca", id, title, title,
            identifiers.GetValueOrDefault("brand"), null, identifiers.GetValueOrDefault("upc"),
            identifiers.GetValueOrDefault("gtin"), identifiers.GetValueOrDefault("mpn"), null,
            price.Amount, original.Amount, price.Currency, null, ParseDate(Text(row, "itemEndDate")), itemUrl,
            Text(row, "itemAffiliateWebUrl"), NestedText(row, "image", "imageUrl"),
            Condition(Text(row, "condition")), NestedText(row, "seller", "username"), true,
            OnlineAvailabilityState.Unknown, "Canada", Shipping(row), Text(row, "categoryPath"), null,
            null, null, fetchedAt, metadata);
    }

    private static Dictionary<string, string> Identifiers(JsonElement row)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (row.TryGetProperty("uniqueProductIdentifiers", out var values) && values.ValueKind == JsonValueKind.Array)
            foreach (var item in values.EnumerateArray())
                foreach (var key in new[] { "brand", "gtin", "upc", "mpn" }) Add(result, key, Text(item, key));
        if (row.TryGetProperty("localizedAspects", out var aspects) && aspects.ValueKind == JsonValueKind.Array)
            foreach (var aspect in aspects.EnumerateArray())
            {
                var name = Text(aspect, "name")?.ToLowerInvariant();
                if (name is "brand" or "mpn" or "upc" or "gtin") Add(result, name, Text(aspect, "value"));
            }
        return result;
    }

    private static string? Shipping(JsonElement row)
    {
        if (!row.TryGetProperty("shippingOptions", out var values) || values.ValueKind != JsonValueKind.Array) return null;
        var first = values.EnumerateArray().FirstOrDefault();
        var cost = Money(first, "shippingCost");
        return cost.Amount.HasValue ? $"{cost.Amount.Value.ToString("0.00", CultureInfo.InvariantCulture)} {cost.Currency}" : null;
    }

    private static ProductCondition Condition(string? value) => value?.ToLowerInvariant() switch
    {
        "new" => ProductCondition.New,
        "used" => ProductCondition.Used,
        var text when text?.Contains("refurb", StringComparison.Ordinal) == true => ProductCondition.Refurbished,
        _ => ProductCondition.Unknown
    };

    private static (decimal? Amount, string? Currency) Money(JsonElement parent, string property)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
            return (null, null);
        return (Decimal(Text(value, "value")), Text(value, "currency")?.ToUpperInvariant());
    }

    private static decimal? Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static int Int(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.TryGetInt32(out var parsed) ? parsed : 0;
    private static string? Text(JsonElement value, string property) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    private static string? NestedText(JsonElement value, string parent, string child) => value.TryGetProperty(parent, out var nested) ? Text(nested, child) : null;
    private static string? JoinArray(JsonElement value, string property) => value.TryGetProperty(property, out var items) && items.ValueKind == JsonValueKind.Array ? string.Join(',', items.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null)) : null;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value) && !values.ContainsKey(key)) values[key] = value.Trim(); }
}
