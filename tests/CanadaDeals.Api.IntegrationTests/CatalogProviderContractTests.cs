using System.Net;
using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Catalogs;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class CatalogProviderContractTests
{
    [Fact]
    public async Task Ebay_client_credentials_token_is_cached_across_concurrent_calls()
    {
        var calls = 0;
        HttpRequestMessage? captured = null;
        string? form = null;
        var provider = new EbayTokenProvider(Client(request =>
        {
            Interlocked.Increment(ref calls);
            captured = Copy(request);
            form = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(HttpStatusCode.OK, "{\"access_token\":\"controlled-ebay-token\",\"expires_in\":7200}");
        }, "https://api.ebay.com"), Options.Create(new EbayCatalogOptions
        {
            Enabled = true, ClientId = "controlled-client", ClientSecret = "controlled-secret", Marketplace = "EBAY_CA"
        }), TimeProvider.System);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => provider.GetAsync(CancellationToken.None)));

        Assert.All(tokens, token => Assert.Equal("controlled-ebay-token", token));
        Assert.Equal(1, calls);
        Assert.Equal("Basic", captured!.Headers.Authorization?.Scheme);
        Assert.DoesNotContain("controlled-secret", captured.Headers.Authorization?.Parameter, StringComparison.Ordinal);
        Assert.Contains("grant_type=client_credentials", form, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ebay_uses_Canadian_marketplace_affiliate_context_and_normalizes_independent_offers()
    {
        HttpRequestMessage? captured = null;
        var source = new EbayCatalogSource(Client(request =>
        {
            captured = Copy(request);
            return Json(HttpStatusCode.OK, Fixture("ebay-catalog-search.json"));
        }, "https://api.ebay.test"), new FixedEbayToken(), Options.Create(new EbayCatalogOptions
        {
            Enabled = true, ApiBaseUrl = "https://api.ebay.test", Marketplace = "EBAY_CA",
            ClientId = "controlled", ClientSecret = "controlled", AffiliateCampaignId = "1234567890",
            AffiliateReferenceId = "privacy-safe-reference"
        }), TimeProvider.System);

        var page = await source.FetchOffersAsync(new CatalogRequest("EBAY_CA", Query: "headphones", PageSize: 2));

        Assert.Equal(2, page.Offers.Count);
        Assert.True(page.HasMore);
        Assert.Equal("EBAY_CA", captured!.Headers.GetValues("X-EBAY-C-MARKETPLACE-ID").Single());
        var context = captured.Headers.GetValues("X-EBAY-C-ENDUSERCTX").Single();
        Assert.Contains("contextualLocation=country%3DCA", context, StringComparison.Ordinal);
        Assert.Contains("affiliateCampaignId=1234567890", context, StringComparison.Ordinal);
        var offer = page.Offers[0];
        Assert.Equal(149.99m, offer.CurrentPrice);
        Assert.Equal(199.99m, offer.RegularPrice);
        Assert.Equal("CAD", offer.Currency);
        Assert.True(offer.Marketplace);
        Assert.Equal("controlled-seller", offer.Seller);
        Assert.Equal(ProductCondition.New, offer.Condition);
        Assert.Equal("00012345678905", offer.Gtin);
        Assert.Equal("NS-100", offer.Mpn);
        Assert.Equal("https://www.ebay.ca/itm/123?campid=controlled", offer.ProviderAffiliateUrl);
    }

    [Fact]
    public async Task Ebay_rejects_non_Canadian_marketplace_and_surfaces_rate_limit_without_unbounded_retry()
    {
        var invalid = new EbayCatalogSource(Client(_ => Json(HttpStatusCode.OK, "{}"), "https://api.ebay.test"),
            new FixedEbayToken(), Options.Create(new EbayCatalogOptions { Enabled = true, Marketplace = "EBAY_US" }), TimeProvider.System);
        await Assert.ThrowsAsync<CatalogProviderException>(() => invalid.FetchOffersAsync(new CatalogRequest("EBAY_US", Query: "test")));

        var limited = new EbayCatalogSource(Client(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        }, "https://api.ebay.test"), new FixedEbayToken(), Options.Create(new EbayCatalogOptions { Enabled = true, Marketplace = "EBAY_CA" }), TimeProvider.System);
        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => limited.FetchOffersAsync(new CatalogRequest("EBAY_CA", Query: "test")));
        Assert.Equal(CatalogFailureKind.RateLimited, error.Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, CatalogFailureKind.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, CatalogFailureKind.Authorization)]
    [InlineData(HttpStatusCode.Found, CatalogFailureKind.InvalidRequest)]
    [InlineData(HttpStatusCode.InternalServerError, CatalogFailureKind.ProviderUnavailable)]
    public async Task Shared_provider_HTTP_boundary_maps_terminal_failures(HttpStatusCode status, CatalogFailureKind expected)
    {
        var source = new EbayCatalogSource(Client(_ => new HttpResponseMessage(status), "https://api.ebay.com"),
            new FixedEbayToken(), Options.Create(new EbayCatalogOptions { Enabled = true, Marketplace = "EBAY_CA" }), TimeProvider.System);

        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => source.FetchOffersAsync(new CatalogRequest("EBAY_CA", Query: "test")));

        Assert.Equal(expected, error.Kind);
    }

    [Fact]
    public async Task Ebay_rejects_malformed_JSON()
    {
        var source = new EbayCatalogSource(Client(_ => Json(HttpStatusCode.OK, "{broken"), "https://api.ebay.com"),
            new FixedEbayToken(), Options.Create(new EbayCatalogOptions { Enabled = true, Marketplace = "EBAY_CA" }), TimeProvider.System);
        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => source.FetchOffersAsync(new CatalogRequest("EBAY_CA", Query: "test")));
        Assert.Equal(CatalogFailureKind.MalformedResponse, error.Kind);
    }

    [Fact]
    public async Task Impact_discovers_accessible_Canadian_catalog_and_maps_catalog_items()
    {
        var requests = new List<HttpRequestMessage>();
        var source = new ImpactCatalogSource(Client(request =>
        {
            requests.Add(Copy(request));
            return request.RequestUri!.AbsolutePath.EndsWith("/Catalogs", StringComparison.Ordinal)
                ? Json(HttpStatusCode.OK, Fixture("impact-catalogs.json"))
                : Json(HttpStatusCode.OK, Fixture("impact-catalog-items.json"));
        }, "https://api.impact.com"), ImpactAffiliateOptions(), Options.Create(new ImpactCatalogOptions { Enabled = true }), TimeProvider.System);

        var discovery = await source.DiscoverAsync(new CatalogDiscoveryRequest());
        var page = await source.FetchOffersAsync(new CatalogRequest("123456", "4321", PageSize: 1));

        var candidate = Assert.Single(discovery);
        Assert.True(candidate.CanadaRelevant);
        Assert.Equal("CAD", candidate.Currency);
        Assert.All(requests, request => Assert.Equal("Basic", request.Headers.Authorization?.Scheme));
        var offer = Assert.Single(page.Offers);
        Assert.Equal("987", offer.ExternalListingId);
        Assert.Equal(899.99m, offer.CurrentPrice);
        Assert.Equal(1099.99m, offer.RegularPrice);
        Assert.Equal("00012345678912", offer.Gtin);
        Assert.Equal(OnlineAvailabilityState.Available, offer.Availability);
        Assert.Null(offer.Marketplace);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task Impact_rejects_malformed_catalog_response()
    {
        var source = new ImpactCatalogSource(Client(_ => Json(HttpStatusCode.OK, "{broken"), "https://api.impact.com"),
            ImpactAffiliateOptions(), Options.Create(new ImpactCatalogOptions { Enabled = true }), TimeProvider.System);
        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => source.DiscoverAsync(new CatalogDiscoveryRequest()));
        Assert.Equal(CatalogFailureKind.MalformedResponse, error.Kind);
    }

    [Fact]
    public async Task Awin_discovers_only_feed_evidence_and_streams_quoted_product_rows()
    {
        var feedListCalls = 0;
        var source = new AwinCatalogSource(Client(request =>
        {
            if (request.RequestUri!.Host == "productdata.awin.com")
            {
                feedListCalls++;
                return Csv(HttpStatusCode.OK, Fixture("awin-feed-list.csv"));
            }
            return Csv(HttpStatusCode.OK, Fixture("awin-products.csv"));
        }, "https://productdata.awin.com"), Options.Create(new AwinCatalogOptions
        {
            Enabled = true, FeedListBaseUrl = "https://productdata.awin.com", DataFeedApiKey = "controlled-key"
        }), TimeProvider.System);

        var discovery = await source.DiscoverAsync(new CatalogDiscoveryRequest());
        var page = await source.FetchOffersAsync(new CatalogRequest("555", "777", PageSize: 10));

        Assert.Equal(2, feedListCalls);
        var candidate = Assert.Single(discovery);
        Assert.Equal(IntegrationPartnershipStatus.Active, candidate.RelationshipStatus);
        Assert.True(candidate.CanadaRelevant);
        var offer = Assert.Single(page.Offers);
        Assert.Equal("Controlled, Awin Television", offer.Title);
        Assert.Equal(499.99m, offer.CurrentPrice);
        Assert.Equal(699.99m, offer.RegularPrice);
        Assert.Equal("00012345678929", offer.Gtin);
        Assert.Null(offer.Marketplace);
        Assert.Equal("https://retailer.example/products/awin-1", offer.DestinationUrl);
        Assert.StartsWith("https://www.awin1.com/", offer.ProviderAffiliateUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Awin_rejects_truncated_quoted_feed_and_unapproved_download_host()
    {
        var source = AwinSource("https://datafeed.api.productserve.com/feed.csv", "header\n\"truncated");
        var malformed = await Assert.ThrowsAsync<CatalogProviderException>(() => source.FetchOffersAsync(new CatalogRequest("555", "777")));
        Assert.Equal(CatalogFailureKind.MalformedResponse, malformed.Kind);

        var unsafeSource = AwinSource("https://attacker.example/feed.csv", Fixture("awin-products.csv"));
        var rejected = await Assert.ThrowsAsync<CatalogProviderException>(() => unsafeSource.FetchOffersAsync(new CatalogRequest("555", "777")));
        Assert.Equal("AWIN_FEED_URL_REJECTED", rejected.SafeCode);
    }

    [Fact]
    public async Task Awin_download_auth_failure_is_terminal()
    {
        var list = Fixture("awin-feed-list.csv");
        var source = new AwinCatalogSource(Client(request => request.RequestUri!.Host == "productdata.awin.com"
            ? Csv(HttpStatusCode.OK, list) : new HttpResponseMessage(HttpStatusCode.Unauthorized), "https://productdata.awin.com"),
            Options.Create(new AwinCatalogOptions { Enabled = true, DataFeedApiKey = "controlled-key" }), TimeProvider.System);
        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => source.FetchOffersAsync(new CatalogRequest("555", "777")));
        Assert.Equal(CatalogFailureKind.Authentication, error.Kind);
    }

    [Fact]
    public async Task Awin_rejects_feed_that_exceeds_the_limit_after_gzip_decompression()
    {
        var list = Fixture("awin-feed-list.csv");
        var oversized = "merchant_id,aw_product_id,product_name,merchant_deep_link,search_price,currency,brand_name,mpn\n" +
            $"555,compressed-1,{new string('A', 4_096)},https://retailer.example/products/compressed-1,10.00,CAD,Controlled,MPN-1\n";
        var compressed = Gzip(Encoding.UTF8.GetBytes(oversized));
        Assert.True(compressed.Length < 512);
        var source = new AwinCatalogSource(Client(request => request.RequestUri!.Host == "productdata.awin.com"
            ? Csv(HttpStatusCode.OK, list) : Bytes(HttpStatusCode.OK, compressed, "application/gzip"), "https://productdata.awin.com"),
            Options.Create(new AwinCatalogOptions
            {
                Enabled = true, DataFeedApiKey = "controlled-key", MaximumFeedBytes = 512
            }), TimeProvider.System);

        var error = await Assert.ThrowsAsync<CatalogProviderException>(() =>
            source.FetchOffersAsync(new CatalogRequest("555", "777")));

        Assert.Equal(CatalogFailureKind.PayloadTooLarge, error.Kind);
        Assert.Equal("AWIN_PAYLOAD_TOO_LARGE", error.SafeCode);
    }

    [Fact]
    public async Task Cj_uses_PAT_and_maps_product_search_without_conflating_link_search()
    {
        HttpRequestMessage? captured = null;
        var source = new CjCatalogSource(Client(request =>
        {
            captured = Copy(request);
            return Xml(HttpStatusCode.OK, Fixture("cj-products.xml"));
        }, "https://product-search.api.cj.test"), CjAffiliateOptions(), Options.Create(new CjCatalogOptions
        {
            Enabled = true, ProductSearchBaseUrl = "https://product-search.api.cj.test", WebsiteId = "controlled-pid",
            CandidateAdvertiserIds = ["888"]
        }), TimeProvider.System);

        var page = await source.FetchOffersAsync(new CatalogRequest("888", PageSize: 1));

        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Contains("/v2/product-search", captured.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        var offer = Assert.Single(page.Offers);
        Assert.Equal(649.99m, offer.CurrentPrice);
        Assert.Equal(799.99m, offer.RegularPrice);
        Assert.Equal("0012345678905", offer.Upc);
        Assert.Null(offer.Marketplace);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task Cj_XML_parser_prohibits_external_entities()
    {
        const string hostile = "<!DOCTYPE root [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><cj-api><products><product><name>&xxe;</name></product></products></cj-api>";
        var source = new CjCatalogSource(Client(_ => Xml(HttpStatusCode.OK, hostile), "https://product-search.api.cj.test"),
            CjAffiliateOptions(), Options.Create(new CjCatalogOptions { Enabled = true, WebsiteId = "controlled-pid" }), TimeProvider.System);

        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => source.FetchOffersAsync(new CatalogRequest("888")));
        Assert.Equal(CatalogFailureKind.MalformedResponse, error.Kind);
    }

    [Fact]
    public async Task Cj_unavailable_product_search_returns_no_candidate_and_rate_limit_is_explicit()
    {
        const string empty = "<cj-api><products total-matched=\"0\" records-returned=\"0\" page-number=\"1\" /></cj-api>";
        var emptySource = new CjCatalogSource(Client(_ => Xml(HttpStatusCode.OK, empty), "https://product-search.api.cj.com"),
            CjAffiliateOptions(), Options.Create(new CjCatalogOptions { Enabled = true, WebsiteId = "controlled-pid", CandidateAdvertiserIds = ["888"] }), TimeProvider.System);
        Assert.Empty(await emptySource.DiscoverAsync(new CatalogDiscoveryRequest()));

        var limited = new CjCatalogSource(Client(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        }, "https://product-search.api.cj.com"), CjAffiliateOptions(), Options.Create(new CjCatalogOptions { Enabled = true, WebsiteId = "controlled-pid" }), TimeProvider.System);
        var error = await Assert.ThrowsAsync<CatalogProviderException>(() => limited.FetchOffersAsync(new CatalogRequest("888")));
        Assert.Equal(CatalogFailureKind.RateLimited, error.Kind);
    }

    [Fact]
    public void Normalized_external_offer_contract_is_provider_neutral_and_bounded()
    {
        var providerNames = typeof(ExternalOffer).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(providerNames, name => name.Contains("Rakuten", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(providerNames, name => name.Contains("Impact", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(providerNames, name => name.Contains("Awin", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("ProviderMetadata", providerNames);
        Assert.Equal(5, CatalogProviderNames.Supported.Count);
    }

    private static AwinCatalogSource AwinSource(string feedUrl, string feedBody)
    {
        var list = Fixture("awin-feed-list.csv").Replace("https://datafeed.api.productserve.com/controlled-feed.csv", feedUrl, StringComparison.Ordinal);
        return new AwinCatalogSource(Client(request => request.RequestUri!.Host == "productdata.awin.com"
            ? Csv(HttpStatusCode.OK, list) : Csv(HttpStatusCode.OK, feedBody), "https://productdata.awin.com"),
            Options.Create(new AwinCatalogOptions { Enabled = true, FeedListBaseUrl = "https://productdata.awin.com", DataFeedApiKey = "controlled-key" }), TimeProvider.System);
    }

    private static IOptions<AffiliateOptions> ImpactAffiliateOptions() => Options.Create(new AffiliateOptions
    {
        Impact = new ImpactAffiliateOptions { Enabled = true, BaseUrl = "https://api.impact.com", AccountSid = "controlled-account", AuthToken = "controlled-token" }
    });
    private static IOptions<AffiliateOptions> CjAffiliateOptions() => Options.Create(new AffiliateOptions
    {
        Cj = new CjAffiliateOptions { Enabled = true, PersonalAccessToken = "controlled-pat" }
    });
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> response, string origin) => new(new DelegateHandler(response)) { BaseAddress = new Uri(origin) };
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Csv(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "text/csv") };
    private static HttpResponseMessage Xml(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };
    private static HttpResponseMessage Bytes(HttpStatusCode status, byte[] body, string mediaType) => new(status) { Content = new ByteArrayContent(body) { Headers = { ContentType = new MediaTypeHeaderValue(mediaType) } } };
    private static byte[] Gzip(byte[] body)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) gzip.Write(body);
        return output.ToArray();
    }
    private static HttpRequestMessage Copy(HttpRequestMessage source)
    {
        var copy = new HttpRequestMessage(source.Method, source.RequestUri);
        foreach (var header in source.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return copy;
    }

    private sealed class FixedEbayToken : IEbayTokenProvider
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult("controlled-token");
    }
    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
