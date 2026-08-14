using System.Net;
using System.Text;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Rakuten;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class RakutenProviderContractTests
{
    [Fact]
    public void Disabled_configuration_is_optional_but_enabled_configuration_requires_account_scope_and_credentials()
    {
        var validator = new RakutenOptionsValidator();

        Assert.True(validator.Validate(null, new RakutenOptions()).Succeeded);
        var invalid = validator.Validate(null, new RakutenOptions { Enabled = true });

        Assert.False(invalid.Succeeded);
        Assert.Contains(invalid.Failures!, failure => failure.Contains("AccountId", StringComparison.Ordinal));
        Assert.Contains(invalid.Failures!, failure => failure.Contains("ClientId", StringComparison.Ordinal));
        Assert.Contains(invalid.Failures!, failure => failure.Contains("ClientSecret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Token_request_uses_token_key_and_account_id_scope_then_caches_for_concurrent_calls()
    {
        HttpRequestMessage? captured = null;
        string? form = null;
        var count = 0;
        var handler = new DelegateHandler(async request =>
        {
            Interlocked.Increment(ref count);
            captured = Copy(request);
            form = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, Fixture("rakuten-token.json"));
        });
        var provider = new RakutenAccessTokenProvider(Client(handler), Options(), TimeProvider.System);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => provider.GetAccessTokenAsync()));

        Assert.All(tokens, token => Assert.Equal("controlled-access-token", token));
        Assert.Equal(1, count);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.NotEqual("controlled-client", captured.Headers.Authorization?.Parameter);
        Assert.Equal("scope=controlled-account", form);
    }

    [Fact]
    public async Task Token_refreshes_before_expiry_with_refresh_token_without_exposing_secret_on_failure()
    {
        var clock = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-08-14T00:00:00Z"));
        var forms = new List<string>();
        var responses = new Queue<HttpResponseMessage>([
            Json(HttpStatusCode.OK, Fixture("rakuten-token.json")),
            Json(HttpStatusCode.OK, Fixture("rakuten-token.json").Replace("controlled-access-token", "controlled-access-token-2"))
        ]);
        var provider = new RakutenAccessTokenProvider(Client(new DelegateHandler(async request =>
        {
            forms.Add(await request.Content!.ReadAsStringAsync());
            return responses.Dequeue();
        })), Options(), clock);

        Assert.Equal("controlled-access-token", await provider.GetAccessTokenAsync());
        clock.Advance(TimeSpan.FromSeconds(3500));
        Assert.Equal("controlled-access-token-2", await provider.GetAccessTokenAsync());
        Assert.Contains("refresh_token=controlled-refresh-token", forms[1]);

        var rejected = new RakutenAccessTokenProvider(Client(new DelegateHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)))), Options(), clock);
        var error = await Assert.ThrowsAsync<RakutenProviderException>(() => rejected.GetAccessTokenAsync());
        Assert.DoesNotContain("controlled-secret", error.ToString(), StringComparison.Ordinal);
        Assert.Equal(RakutenFailureKind.Authentication, error.Kind);
    }

    [Fact]
    public async Task Token_rejects_malformed_response()
    {
        var provider = new RakutenAccessTokenProvider(Client(new DelegateHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, "{broken")))), Options(), TimeProvider.System);

        var error = await Assert.ThrowsAsync<RakutenProviderException>(() => provider.GetAccessTokenAsync());

        Assert.Equal(RakutenFailureKind.MalformedResponse, error.Kind);
    }

    [Fact]
    public async Task Advertisers_and_partnerships_parse_capabilities_and_fail_closed_unknown_states()
    {
        var advertiserClient = new RakutenAdvertiserClient(Authenticated(new DelegateHandler(_ =>
            Task.FromResult(Json(HttpStatusCode.OK, Fixture("rakuten-advertisers.json"))))));
        var partnershipClient = new RakutenPartnershipClient(Authenticated(new DelegateHandler(_ =>
            Task.FromResult(Json(HttpStatusCode.OK, Fixture("rakuten-partnerships.json"))))));

        var advertisers = await advertiserClient.GetAllAsync();
        var partnerships = await partnershipClient.GetAllAsync();

        Assert.Equal(2, advertisers.Count);
        Assert.True(advertisers[0].ProductFeedAvailable);
        Assert.True(advertisers[0].DeepLinksAvailable);
        Assert.Contains("CA", advertisers[0].ShipsTo);
        Assert.Equal(IntegrationPartnershipStatus.Active, partnerships[0].PartnershipStatus);
        Assert.Equal(IntegrationAdvertiserStatus.Inactive, partnerships[1].AdvertiserStatus);
        Assert.Equal(IntegrationPartnershipStatus.Unknown, RakutenPartnershipClient.ParsePartnershipStatus("new-provider-state"));
    }

    [Fact]
    public async Task Product_search_is_mid_scoped_and_parses_safe_xml_price_semantics()
    {
        HttpRequestMessage? captured = null;
        var client = new RakutenProductSearchClient(Authenticated(new DelegateHandler(request =>
        {
            captured = Copy(request);
            return Task.FromResult(Xml(HttpStatusCode.OK, Fixture("rakuten-products.xml")));
        })));

        var page = await client.GetPageAsync("101", 1, 20);

        Assert.Contains("mid=101", captured!.RequestUri!.Query);
        Assert.Contains("pagenumber=1", captured.RequestUri.Query);
        Assert.Equal(2, page.Products.Count);
        Assert.Equal((1099.99m, "CAD"), page.Products[0].CurrentPrice());
        Assert.Equal("USD", page.Products[1].CurrentPrice().Currency);
        Assert.DoesNotContain(typeof(RakutenProductRecord).GetProperties(), property =>
            property.Name.Contains("Commission", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Epc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Product_search_rejects_dtd_and_invalid_xml()
    {
        const string hostile = "<!DOCTYPE result [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><result><item>&xxe;</item></result>";
        var client = new RakutenProductSearchClient(Authenticated(new DelegateHandler(_ => Task.FromResult(Xml(HttpStatusCode.OK, hostile)))));

        var error = await Assert.ThrowsAsync<RakutenProviderException>(() => client.GetPageAsync("101", 1, 10));

        Assert.Equal(RakutenFailureKind.MalformedResponse, error.Kind);
    }

    [Fact]
    public async Task Deep_link_uses_provider_contract_and_opaque_u1()
    {
        string? body = null;
        var client = new RakutenDeepLinkClient(Authenticated(new DelegateHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, Fixture("rakuten-deep-link.json"));
        })));

        var result = await client.CreateAsync("101", "https://merchant.safe.test/products/qled-tv", "listing_controlled");

        Assert.Equal("https://click.linksynergy.test/deeplink?id=controlled", result.TrackingUrl);
        Assert.Contains("\"advertiser_id\":101", body);
        Assert.Contains("\"u1\":\"listing_controlled\"", body);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ACCESS_DENIED", RakutenFailureKind.PartnershipDenied)]
    [InlineData("DEEP_LINK_DENIED", RakutenFailureKind.DeepLinkDisabled)]
    [InlineData("CANNOT_RESOLVE_ADVERTISER", RakutenFailureKind.AdvertiserInactive)]
    [InlineData("DEEP_LINKING_NOT_ENABLED", RakutenFailureKind.DeepLinkDisabled)]
    [InlineData("URL_TEMPLATE_MISMATCH", RakutenFailureKind.InvalidDestination)]
    public async Task Deep_link_maps_provider_errors(string providerCode, RakutenFailureKind expected)
    {
        var client = new RakutenDeepLinkClient(Authenticated(new DelegateHandler(_ =>
            Task.FromResult(Json(HttpStatusCode.OK, $"{{\"error\":\"{providerCode}\"}}")))));

        var error = await Assert.ThrowsAsync<RakutenProviderException>(() =>
            client.CreateAsync("101", "https://merchant.safe.test/product", "opaque"));

        Assert.Equal(expected, error.Kind);
    }

    [Fact]
    public async Task Affiliate_adapter_rejects_wrong_tracking_host_after_capability_gate()
    {
        var provider = new RakutenAffiliateLinkProvider(
            new FakeDeepLinks("https://attacker.example/click"), new AllowedGate(), Options(), TimeProvider.System);

        var result = await provider.ResolveAsync(RakutenAffiliateRequest());

        Assert.Equal(AffiliateResolutionStatus.InvalidTrackingUrl, result.Status);
        Assert.Null(result.TrackingUrl);
    }

    private static RakutenAuthenticatedClient Authenticated(HttpMessageHandler handler) => new(
        Client(handler), new FixedTokenProvider(), new RakutenRequestGate(Microsoft.Extensions.Options.Options.Create(new RakutenOptions
        {
            MinimumRequestIntervalMilliseconds = 0
        }), TimeProvider.System), NullLogger<RakutenAuthenticatedClient>.Instance);

    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://api.linksynergy.test/") };
    private static IOptions<RakutenOptions> Options() => Microsoft.Extensions.Options.Options.Create(new RakutenOptions
    {
        Enabled = true, LiveDiscoveryEnabled = true, CatalogImportEnabled = true, DeepLinkEnabled = true,
        ApiBaseUrl = "https://api.linksynergy.test", AccountId = "controlled-account",
        ClientId = "controlled-client", ClientSecret = "controlled-secret", MinimumRequestIntervalMilliseconds = 0
    });
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Xml(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };
    private static HttpRequestMessage Copy(HttpRequestMessage request)
    {
        var copy = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return copy;
    }

    private static AffiliateLinkRequest RakutenAffiliateRequest()
    {
        var now = DateTimeOffset.UtcNow;
        var retailer = Retailer.Create("rakuten-controlled", "Rakuten Controlled");
        var program = AffiliateProgram.Create(retailer.Id, AffiliateProviderType.Rakuten, AffiliateProgramStatus.Active, now,
            "101", null, null, true, ["merchant.safe.test"], ["linksynergy.test"], "controlled-evidence", now);
        var listing = RetailerListing.Create(Guid.NewGuid(), retailer.Id, "controlled-listing", "Controlled", "https://merchant.safe.test/product",
            Guid.NewGuid(), MatchState.AutoMatched, now, now, 10m, "CAD", FreshnessState.Recent, EvidenceState.Unknown,
            HistoryAvailability.Unavailable, approvedAffiliateDestinationReference: "https://merchant.safe.test/product", condition: ProductCondition.Unknown,
            onlineAvailability: OnlineAvailabilityState.Unknown);
        return new AffiliateLinkRequest(program, retailer, listing, "product-page", "listing_controlled");
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(request);
    }
    private sealed class FixedTokenProvider : IRakutenAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("controlled-token");
        public void Invalidate() { }
    }
    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
    private sealed class FakeDeepLinks(string trackingUrl) : IRakutenDeepLinkClient
    {
        public Task<RakutenDeepLinkResult> CreateAsync(string advertiserMid, string destinationUrl, string? opaqueAttribution, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RakutenDeepLinkResult(advertiserMid, trackingUrl, destinationUrl, opaqueAttribution));
    }
    private sealed class AllowedGate : IRakutenCapabilityGate
    {
        public Task<(bool Eligible, string Reason)> CanGenerateAffiliateLinkAsync(string advertiserMid, Guid retailerId, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, "CONTROLLED"));
    }
}
