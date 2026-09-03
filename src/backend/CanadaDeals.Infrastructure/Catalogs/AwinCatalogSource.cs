using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using CanadaDeals.Domain.Common;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class AwinCatalogOptions
{
    public const string SectionName = "CatalogProviders:Awin";
    public bool Enabled { get; init; }
    public string FeedListBaseUrl { get; init; } = "https://productdata.awin.com";
    public string? DataFeedApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaximumFeedBytes { get; init; } = 32 * 1024 * 1024;
}

public sealed class AwinCatalogSource(HttpClient client, IOptions<AwinCatalogOptions> options, TimeProvider clock) : IOfferCatalogSource
{
    private static readonly string[] AllowedFeedHosts = ["productdata.awin.com", "datafeed.api.productserve.com"];
    public string Provider => CatalogProviderNames.Awin;

    public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogCapabilities(true, false, false, true, true, true, 1000, "awin-product-feed-list-2025"));

    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var feeds = await GetFeedsAsync(cancellationToken);
        return feeds.Take(request.MaximumCandidates).Select(feed =>
        {
            var joined = string.Equals(feed.GetValueOrDefault("Membership Status"), "Joined", StringComparison.OrdinalIgnoreCase);
            var region = feed.GetValueOrDefault("Primary Region");
            var advertiserId = feed.GetValueOrDefault("Advertiser ID") ?? string.Empty;
            var feedId = feed.GetValueOrDefault("Feed ID");
            var metadata = new Dictionary<string, string>();
            Add(metadata, "feedName", feed.GetValueOrDefault("Feed Name"));
            Add(metadata, "language", feed.GetValueOrDefault("Language"));
            Add(metadata, "primaryRegion", region);
            return new CatalogCandidate(Provider, advertiserId, feedId,
                feed.GetValueOrDefault("Advertiser Name") ?? advertiserId,
                joined ? IntegrationPartnershipStatus.Active : IntegrationPartnershipStatus.Unknown,
                !string.IsNullOrWhiteSpace(feedId), joined,
                string.Equals(region, "CA", StringComparison.OrdinalIgnoreCase), null,
                ParseDate(feed.GetValueOrDefault("Last Imported")), metadata);
        }).Where(candidate => candidate.ProviderAdvertiserId.Length > 0 && !string.IsNullOrWhiteSpace(candidate.CatalogId)).ToArray();
    }

    public async Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CatalogId))
            throw new CatalogProviderException(CatalogFailureKind.InvalidRequest, "AWIN_FEED_ID_REQUIRED");
        var feeds = await GetFeedsAsync(cancellationToken);
        var feed = feeds.SingleOrDefault(row =>
            string.Equals(row.GetValueOrDefault("Advertiser ID"), request.ProviderAdvertiserId, StringComparison.Ordinal) &&
            string.Equals(row.GetValueOrDefault("Feed ID"), request.CatalogId, StringComparison.Ordinal));
        if (feed is null || !string.Equals(feed.GetValueOrDefault("Membership Status"), "Joined", StringComparison.OrdinalIgnoreCase))
            throw new CatalogProviderException(CatalogFailureKind.RelationshipDenied, "AWIN_FEED_NOT_JOINED_OR_UNAVAILABLE");
        var feedUrl = feed.GetValueOrDefault("URL");
        if (!TryApprovedFeedUrl(feedUrl, out var approvedUrl))
            throw new CatalogProviderException(CatalogFailureKind.Authorization, "AWIN_FEED_URL_REJECTED");

        using var response = await CatalogHttp.SendAsync(client,
            () => new HttpRequestMessage(HttpMethod.Get, approvedUrl), "AWIN", cancellationToken);
        await using var boundedDownload = await CatalogHttp.BoundedStreamAsync(response, options.Value.MaximumFeedBytes, "AWIN", cancellationToken);
        await using var decoded = await MaybeGzipAsync(boundedDownload, cancellationToken);
        await using var boundedDecoded = CatalogHttp.Bound(decoded, options.Value.MaximumFeedBytes, "AWIN");
        using var reader = new StreamReader(boundedDecoded, Encoding.UTF8, true, 32 * 1024, leaveOpen: false);
        var rows = CsvRows(reader, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            if (!await rows.MoveNextAsync()) throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "AWIN_FEED_HEADER_MISSING");
            var header = rows.Current;
            var offset = Math.Max(0, request.PageNumber - 1) * Math.Clamp(request.PageSize, 1, 1000);
            var skipped = 0;
            var offers = new List<ExternalOffer>();
            var pageSize = Math.Clamp(request.PageSize, 1, 1000);
            var hasMore = false;
            while (await rows.MoveNextAsync())
            {
                var values = rows.Current;
                if (values.Count != header.Count) continue;
                if (skipped++ < offset) continue;
                if (offers.Count >= pageSize) { hasMore = true; break; }
                var row = header.Select((name, index) => (name, value: values[index])).ToDictionary(pair => pair.name, pair => pair.value, StringComparer.OrdinalIgnoreCase);
                var mapped = Map(row, request.ProviderAdvertiserId, clock.GetUtcNow());
                if (mapped is not null) offers.Add(mapped);
            }
            return new CatalogPage(request.PageNumber, offset + offers.Count + (hasMore ? 1 : 0), offers, hasMore);
        }
        finally { await rows.DisposeAsync(); }
    }

    private async Task<IReadOnlyList<Dictionary<string, string>>> GetFeedsAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.DataFeedApiKey))
            throw new CatalogProviderException(CatalogFailureKind.Configuration, "AWIN_CATALOG_CONFIGURATION_DISABLED");
        var path = $"/datafeed/list/apikey/{Uri.EscapeDataString(config.DataFeedApiKey)}";
        using var response = await CatalogHttp.SendAsync(client, () => new HttpRequestMessage(HttpMethod.Get, path), "AWIN", cancellationToken);
        await using var stream = await CatalogHttp.BoundedStreamAsync(response, 16 * 1024 * 1024, "AWIN", cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 16 * 1024, leaveOpen: false);
        var enumerator = CsvRows(reader, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            if (!await enumerator.MoveNextAsync()) throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "AWIN_FEED_LIST_HEADER_MISSING");
            var header = enumerator.Current;
            var result = new List<Dictionary<string, string>>();
            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current.Count != header.Count) continue;
                result.Add(header.Select((name, index) => (name, value: enumerator.Current[index]))
                    .ToDictionary(pair => pair.name.Trim(), pair => pair.value.Trim(), StringComparer.OrdinalIgnoreCase));
            }
            return result;
        }
        finally { await enumerator.DisposeAsync(); }
    }

    private static ExternalOffer? Map(IReadOnlyDictionary<string, string> row, string advertiserId, DateTimeOffset fetchedAt)
    {
        if (!string.Equals(Value(row, "merchant_id", "advertiser_id"), advertiserId, StringComparison.Ordinal)) return null;
        var id = Value(row, "aw_product_id", "merchant_product_id");
        var title = Value(row, "product_name");
        var destination = Value(row, "merchant_deep_link");
        if (id is null || title is null || destination is null) return null;
        var current = Decimal(Value(row, "search_price", "store_price"));
        var regular = Decimal(Value(row, "product_price_old", "rrp_price"));
        var inStock = Value(row, "in_stock", "stock_status")?.ToLowerInvariant();
        var availability = inStock switch
        {
            "1" or "true" or "yes" or "in stock" or "instock" => OnlineAvailabilityState.Available,
            "0" or "false" or "no" or "out of stock" or "outofstock" => OnlineAvailabilityState.Unavailable,
            _ => OnlineAvailabilityState.Unknown
        };
        var condition = Value(row, "condition")?.ToLowerInvariant() switch
        {
            "new" => ProductCondition.New,
            "used" => ProductCondition.Used,
            var text when text?.Contains("refurb", StringComparison.Ordinal) == true => ProductCondition.Refurbished,
            _ => ProductCondition.Unknown
        };
        var metadata = new Dictionary<string, string>();
        Add(metadata, "feedLastUpdated", Value(row, "last_updated"));
        Add(metadata, "stockStatus", Value(row, "stock_status"));
        return new ExternalOffer(CatalogProviderNames.Awin, advertiserId, null, id, title, title,
            Value(row, "brand_name"), Value(row, "merchant_product_id"), Value(row, "upc"),
            Value(row, "product_GTIN", "ean"), Value(row, "mpn"), Value(row, "model_number", "product_model"),
            current, regular, Value(row, "currency")?.ToUpperInvariant(), ParseDate(Value(row, "valid_from")),
            ParseDate(Value(row, "valid_to")), destination, Value(row, "aw_deep_link"),
            Value(row, "merchant_image_url", "aw_image_url", "large_image"), condition, null, null,
            availability, null, Value(row, "delivery_cost", "delivery_time"),
            Value(row, "merchant_category", "category_name"), Value(row, "merchant_product_second_category"),
            null, ParseDate(Value(row, "last_updated")), fetchedAt, metadata);
    }

    internal static async IAsyncEnumerable<IReadOnlyList<string>> CsvRows(
        TextReader reader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var quoteClosed = false;
        var buffer = new char[1];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                if (quoted) throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "AWIN_FEED_TRUNCATED_QUOTED_FIELD");
                if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row; }
                yield break;
            }
            var character = buffer[0];
            if (quoted)
            {
                if (character == '"') { quoted = false; quoteClosed = true; }
                else field.Append(character);
                continue;
            }
            if (quoteClosed)
            {
                if (character == '"') { field.Append('"'); quoted = true; quoteClosed = false; continue; }
                if (character == ',') { row.Add(field.ToString()); field.Clear(); quoteClosed = false; continue; }
                if (character == '\n') { row.Add(field.ToString()); yield return row; row = []; field.Clear(); quoteClosed = false; continue; }
                if (character == '\r' || char.IsWhiteSpace(character)) continue;
                throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "AWIN_FEED_INVALID_QUOTING");
            }
            if (character == '"' && field.Length == 0) { quoted = true; continue; }
            if (character == ',') { row.Add(field.ToString()); field.Clear(); continue; }
            if (character == '\n') { row.Add(field.ToString()); yield return row; row = []; field.Clear(); continue; }
            if (character != '\r') field.Append(character);
        }
    }

    private static bool TryApprovedFeedUrl(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(parsed.UserInfo) || !AllowedFeedHosts.Contains(parsed.IdnHost, StringComparer.OrdinalIgnoreCase)) return false;
        uri = parsed;
        return true;
    }

    private static async Task<Stream> MaybeGzipAsync(Stream stream, CancellationToken cancellationToken)
    {
        var prefix = new byte[2];
        var read = await stream.ReadAsync(prefix, cancellationToken);
        var replay = new PrefixStream(prefix.AsMemory(0, read).ToArray(), stream);
        return read == 2 && prefix[0] == 0x1f && prefix[1] == 0x8b
            ? new GZipStream(replay, CompressionMode.Decompress)
            : replay;
    }

    private static string? Value(IReadOnlyDictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys) if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        return null;
    }
    private static decimal? Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim(); }

    private sealed class PrefixStream(byte[] prefix, Stream inner) : Stream
    {
        private int _offset;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _offset; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var copied = CopyPrefix(buffer.AsSpan(offset, count));
            return copied > 0 ? copied : inner.Read(buffer, offset, count);
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var copied = CopyPrefix(buffer.Span);
            return copied > 0 ? copied : await inner.ReadAsync(buffer, cancellationToken);
        }
        private int CopyPrefix(Span<byte> destination)
        {
            var count = Math.Min(destination.Length, prefix.Length - _offset);
            if (count <= 0) return 0;
            prefix.AsSpan(_offset, count).CopyTo(destination);
            _offset += count;
            return count;
        }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await inner.DisposeAsync(); await base.DisposeAsync(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
