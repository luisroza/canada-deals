using System.Net;
using System.Text;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class AffiliateProviderContractTests
{
    [Fact]
    public void Disabled_providers_require_no_credentials_but_enabled_provider_fails_closed()
    {
        var validator = new AffiliateOptionsValidator();

        Assert.True(validator.Validate(null, new AffiliateOptions()).Succeeded);
        var invalid = validator.Validate(null, new AffiliateOptions
        {
            Impact = new ImpactAffiliateOptions { Enabled = true }
        });
        Assert.False(invalid.Succeeded);
        Assert.Contains(invalid.Failures!, failure => failure.Contains("AccountSid", StringComparison.Ordinal));
        Assert.Contains(invalid.Failures!, failure => failure.Contains("AuthToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Impact_validates_active_relationship_and_creates_regular_non_pii_tracking_link()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = new QueueHandler(
            _ => Json(HttpStatusCode.OK, Fixture("impact-active-program.json")),
            _ => Json(HttpStatusCode.OK, Fixture("impact-tracking-link.json")),
            requests);
        var provider = new ImpactAffiliateLinkProvider(new HttpClient(handler), ImpactOptions(), TimeProvider.System);

        var result = await provider.ResolveAsync(ImpactRequest());

        Assert.Equal(AffiliateResolutionStatus.Success, result.Status);
        Assert.Equal("https://canadadeals.sjv.io/c/fake-partner/fake-ad/fake-program", result.TrackingUrl);
        Assert.NotNull(requests[0].Headers.Authorization);
        Assert.NotNull(requests[1].RequestUri);
        Assert.Equal("Basic", requests[0].Headers.Authorization?.Scheme);
        Assert.Equal(HttpMethod.Post, requests[1].Method);
        Assert.Contains("Type=Regular", requests[1].RequestUri!.Query);
        Assert.Contains("MediaPartnerPropertyId=fake-property", requests[1].RequestUri!.Query);
        Assert.Contains("subId1=product-page", requests[1].RequestUri!.Query);
        Assert.DoesNotContain("email", requests[1].RequestUri!.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Impact_denies_link_when_provider_disallows_deeplinking()
    {
        var relationship = Fixture("impact-active-program.json").Replace("\"AllowsDeeplinking\": true", "\"AllowsDeeplinking\": false");
        var provider = ImpactProvider(_ => Json(HttpStatusCode.OK, relationship));

        var result = await provider.ResolveAsync(ImpactRequest());

        Assert.Equal(AffiliateResolutionStatus.DeepLinkForbidden, result.Status);
    }

    [Fact]
    public async Task Impact_denies_destination_absent_from_authoritative_program_domains()
    {
        var relationship = Fixture("impact-active-program.json").Replace("bestbuy.ca", "example.invalid");
        var provider = ImpactProvider(_ => Json(HttpStatusCode.OK, relationship));

        var result = await provider.ResolveAsync(ImpactRequest());

        Assert.Equal(AffiliateResolutionStatus.InvalidDestination, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AffiliateResolutionStatus.AuthenticationFailed)]
    [InlineData(HttpStatusCode.Forbidden, AffiliateResolutionStatus.RelationshipInactive)]
    [InlineData((HttpStatusCode)429, AffiliateResolutionStatus.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AffiliateResolutionStatus.TemporaryFailure)]
    public async Task Impact_classifies_provider_failures(HttpStatusCode status, AffiliateResolutionStatus expected)
    {
        var provider = ImpactProvider(_ => new HttpResponseMessage(status));

        var result = await provider.ResolveAsync(ImpactRequest());

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Impact_rejects_malformed_tracking_url()
    {
        var provider = new ImpactAffiliateLinkProvider(new HttpClient(new QueueHandler(
            _ => Json(HttpStatusCode.OK, Fixture("impact-active-program.json")),
            _ => Json(HttpStatusCode.OK, "{\"TrackingURL\":\"javascript:alert(1)\"}"))), ImpactOptions(), TimeProvider.System);

        var result = await provider.ResolveAsync(ImpactRequest());

        Assert.Equal(AffiliateResolutionStatus.InvalidTrackingUrl, result.Status);
    }

    [Fact]
    public async Task Cj_uses_pat_and_accepts_only_joined_deeplink_capable_provider_returned_link()
    {
        var requests = new List<HttpRequestMessage>();
        var provider = new CjAffiliateLinkProvider(new HttpClient(new QueueHandler(
            _ => Xml(HttpStatusCode.OK, Fixture("cj-joined-link.xml")), requests)), CjOptions(), TimeProvider.System);

        var result = await provider.ResolveAsync(CjRequest());

        Assert.Equal(AffiliateResolutionStatus.Success, result.Status);
        Assert.Equal("https://www.tkqlhce.com/click-fake-property-fake-link", result.TrackingUrl);
        Assert.NotNull(requests[0].Headers.Authorization);
        Assert.NotNull(requests[0].RequestUri);
        Assert.Equal("Bearer", requests[0].Headers.Authorization?.Scheme);
        Assert.Contains("website-id=fake-property", requests[0].RequestUri!.Query);
        Assert.Contains("advertiser-ids=fake-advertiser", requests[0].RequestUri!.Query);
        Assert.Contains("link-id=fake-link", requests[0].RequestUri!.Query);
        Assert.Contains("allow-deep-linking=true", requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task Cj_blocks_not_joined_relationship()
    {
        var xml = Fixture("cj-joined-link.xml").Replace("<relationship-status>joined</relationship-status>", "<relationship-status>notjoined</relationship-status>");
        var provider = CjProvider(_ => Xml(HttpStatusCode.OK, xml));

        var result = await provider.ResolveAsync(CjRequest());

        Assert.Equal(AffiliateResolutionStatus.RelationshipInactive, result.Status);
    }

    [Fact]
    public async Task Cj_blocks_link_without_deeplink_permission()
    {
        var xml = Fixture("cj-joined-link.xml").Replace("<allow-deep-linking>true</allow-deep-linking>", "<allow-deep-linking>false</allow-deep-linking>");
        var provider = CjProvider(_ => Xml(HttpStatusCode.OK, xml));

        var result = await provider.ResolveAsync(CjRequest());

        Assert.Equal(AffiliateResolutionStatus.DeepLinkForbidden, result.Status);
    }

    [Fact]
    public async Task Cj_classifies_rate_limit_without_fabricating_a_link()
    {
        var provider = CjProvider(_ => new HttpResponseMessage((HttpStatusCode)429));

        var result = await provider.ResolveAsync(CjRequest());

        Assert.Equal(AffiliateResolutionStatus.RateLimited, result.Status);
        Assert.Null(result.TrackingUrl);
    }

    [Fact]
    public async Task Cj_rejects_malformed_response()
    {
        var provider = CjProvider(_ => Xml(HttpStatusCode.OK, "<cj-api><broken>"));

        var result = await provider.ResolveAsync(CjRequest());

        Assert.Equal(AffiliateResolutionStatus.MalformedResponse, result.Status);
    }

    private static ImpactAffiliateLinkProvider ImpactProvider(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new HttpClient(new QueueHandler(response)), ImpactOptions(), TimeProvider.System);

    private static CjAffiliateLinkProvider CjProvider(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new HttpClient(new QueueHandler(response)), CjOptions(), TimeProvider.System);

    private static IOptions<AffiliateOptions> ImpactOptions() => Options.Create(new AffiliateOptions
    {
        Impact = new ImpactAffiliateOptions { Enabled = true, BaseUrl = "https://api.impact.test", AccountSid = "fake-account", AuthToken = "fake-token" }
    });

    private static IOptions<AffiliateOptions> CjOptions() => Options.Create(new AffiliateOptions
    {
        Cj = new CjAffiliateOptions { Enabled = true, BaseUrl = "https://link-search.api.cj.test", PersonalAccessToken = "fake-pat" }
    });

    private static AffiliateLinkRequest ImpactRequest() => Request(AffiliateProviderType.Impact, "fake-program", "fake-property", null,
        "https://www.bestbuy.ca/product/fake", ["bestbuy.ca"], ["sjv.io"]);

    private static AffiliateLinkRequest CjRequest() => Request(AffiliateProviderType.Cj, "fake-advertiser", "fake-property", "fake-link",
        "https://www.homedepot.ca/product/fake-product", ["homedepot.ca"], ["tkqlhce.com"]);

    private static AffiliateLinkRequest Request(AffiliateProviderType provider, string programId, string propertyId, string? linkReference,
        string destination, string[] destinationDomains, string[] trackingDomains)
    {
        var now = DateTimeOffset.UtcNow;
        var retailer = Retailer.Create($"fixture-{Guid.NewGuid():N}", "Fixture Retailer");
        var program = AffiliateProgram.Create(retailer.Id, provider, AffiliateProgramStatus.Active, now,
            programId, propertyId, linkReference, true, destinationDomains, trackingDomains, "fixture-evidence", now);
        var listing = RetailerListing.Create(Guid.NewGuid(), retailer.Id, $"listing-{Guid.NewGuid():N}", "Fixture listing", destination,
            Guid.NewGuid(), MatchState.Confirmed, now, now, 10m, "CAD", FreshnessState.Recent, EvidenceState.Strong,
            HistoryAvailability.Unavailable, approvedAffiliateDestinationReference: destination);
        return new AffiliateLinkRequest(program, retailer, listing, "product-page", $"listing-{listing.Id:N}");
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Xml(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
        private readonly List<HttpRequestMessage>? _requests;

        public QueueHandler(Func<HttpRequestMessage, HttpResponseMessage> response, List<HttpRequestMessage>? requests = null)
            : this([response], requests) { }

        public QueueHandler(Func<HttpRequestMessage, HttpResponseMessage> first, Func<HttpRequestMessage, HttpResponseMessage> second,
            List<HttpRequestMessage>? requests = null) : this([first, second], requests) { }

        private QueueHandler(IEnumerable<Func<HttpRequestMessage, HttpResponseMessage>> responses, List<HttpRequestMessage>? requests)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
            _requests = requests;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_requests is not null)
            {
                var copy = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                _requests.Add(copy);
            }
            if (_responses.Count == 0) throw new InvalidOperationException("No controlled provider response remains.");
            return Task.FromResult(_responses.Dequeue()(request));
        }
    }
}
