using System.Net;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Infrastructure.Rakuten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class AffiliateHandoffIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient Client() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    [RequiresPostgresFact]
    public async Task Valid_active_persisted_link_redirects_and_creates_minimal_click_event()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active);

        using var response = await Client().GetAsync($"/go/{scenario.ListingId}?destination=https://attacker.example");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://tracking.safe.test/click/fixture", response.Headers.Location?.ToString());
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var click = await db.ClickEvents.SingleAsync(x => x.RetailerListingId == scenario.ListingId);
        Assert.Equal(scenario.LinkId, click.AffiliateLinkId);
        Assert.Equal("product-page", click.Placement);
        Assert.DoesNotContain(typeof(ClickEvent).GetProperties(), property =>
            property.Name.Contains("Ip", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("User", StringComparison.OrdinalIgnoreCase));
    }

    [RequiresPostgresFact]
    public async Task Missing_or_suspended_program_blocks_handoff()
    {
        var suspended = await CreateScenarioAsync(AffiliateProgramStatus.Suspended);
        var pending = await CreateScenarioAsync(AffiliateProgramStatus.PendingApproval);

        using var suspendedResponse = await Client().GetAsync($"/go/{suspended.ListingId}");
        using var pendingResponse = await Client().GetAsync($"/go/{pending.ListingId}");

        Assert.Equal(HttpStatusCode.NotFound, suspendedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, pendingResponse.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Invalid_provider_tracking_host_fails_closed()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active,
            trackingUrl: "https://attacker.example/click", trackingDomains: ["tracking.safe.test"]);

        using var response = await Client().GetAsync($"/go/{scenario.ListingId}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Invalid_retailer_destination_fails_closed()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active,
            destinationUrl: "https://attacker.example/product", destinationDomains: ["merchant.safe.test"]);

        using var response = await Client().GetAsync($"/go/{scenario.ListingId}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Provider_outage_does_not_affect_an_existing_valid_persisted_link()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active, provider: AffiliateProviderType.Impact);

        using var response = await Client().GetAsync($"/go/{scenario.ListingId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Rakuten_handoff_fails_closed_when_persisted_partnership_is_removed()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active, provider: AffiliateProviderType.Rakuten);

        using var activeResponse = await Client().GetAsync($"/go/{scenario.ListingId}");
        Assert.Equal(HttpStatusCode.Redirect, activeResponse.StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var capability = await db.RakutenAdvertiserCapabilities.SingleAsync(candidate => candidate.AdvertiserMid == scenario.ProviderProgramId);
            var now = DateTimeOffset.UtcNow;
            capability.ReconcileProviderSnapshot("Controlled Rakuten merchant", "https://merchant.safe.test",
                IntegrationAdvertiserStatus.Active, IntegrationPartnershipStatus.TemporaryRemove, ["CA"], true, true,
                now, now.AddDays(-1), now);
            await db.SaveChangesAsync();
        }

        using var removedResponse = await Client().GetAsync($"/go/{scenario.ListingId}");
        Assert.Equal(HttpStatusCode.NotFound, removedResponse.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Discovery_reconciliation_suspends_program_and_disables_links()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active, provider: AffiliateProviderType.Rakuten);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var service = new RakutenDiscoveryService(
            new FakePartnershipClient([new RakutenPartnershipRecord(scenario.ProviderProgramId, "Controlled Rakuten merchant",
                IntegrationAdvertiserStatus.Active, IntegrationPartnershipStatus.TemporaryRemove, now.AddDays(-1), now)]),
            new FakeAdvertiserClient([new RakutenAdvertiserRecord(scenario.ProviderProgramId, "Controlled Rakuten merchant",
                "https://merchant.safe.test", true, ["CA"], true, true)]),
            db, Options.Create(new RakutenOptions { Enabled = true, LiveDiscoveryEnabled = true }), TimeProvider.System);

        await service.DiscoverAsync(true);

        var program = await db.AffiliatePrograms.SingleAsync(candidate => candidate.Id == scenario.ProgramId);
        var link = await db.AffiliateLinks.SingleAsync(candidate => candidate.Id == scenario.LinkId);
        Assert.Equal(AffiliateProgramStatus.Suspended, program.Status);
        Assert.Equal(AffiliateLinkStatus.Disabled, link.Status);
        Assert.Equal("RAKUTEN_RELATIONSHIP_INACTIVE", link.FailureReason);
        await transaction.RollbackAsync();
    }

    [RequiresPostgresFact]
    public async Task Deterministic_provider_refresh_persists_link_then_go_redirects()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active, provider: AffiliateProviderType.Impact, createLink: false);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var service = new AffiliateLinkRefreshService(db, [new DeterministicProvider()],
                Options.Create(new AffiliateOptions { RevalidateHours = 168, FailureRetryMinutes = 60 }),
                TimeProvider.System, NullLogger<AffiliateLinkRefreshService>.Instance);

            var summary = await service.RefreshDueAsync(scenario.ListingId);

            Assert.Equal(1, summary.Refreshed);
            Assert.Equal(0, summary.Failed);
            Assert.True(await db.AffiliateLinks.AnyAsync(link => link.RetailerListingId == scenario.ListingId && link.Status == AffiliateLinkStatus.Active));
        }

        using var response = await Client().GetAsync($"/go/{scenario.ListingId}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://tracking.safe.test/click/refreshed", response.Headers.Location?.ToString());
    }

    private async Task<(Guid ListingId, Guid LinkId, Guid ProgramId, string ProviderProgramId)> CreateScenarioAsync(
        AffiliateProgramStatus status,
        string destinationUrl = "https://merchant.safe.test/product/fixture",
        string trackingUrl = "https://tracking.safe.test/click/fixture",
        string[]? destinationDomains = null,
        string[]? trackingDomains = null,
        AffiliateProviderType provider = AffiliateProviderType.Other,
        bool createLink = true)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var brand = await db.Brands.FirstAsync();
        var category = await db.Categories.FirstAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var retailer = Retailer.Create($"affiliate-{suffix}", $"Affiliate fixture {suffix}");
        var now = DateTimeOffset.UtcNow;
        var policy = provider == AffiliateProviderType.Rakuten
            ? MerchantPolicy.Create($"affiliate-rakuten-{suffix}", PolicyPermission.Allowed, PolicyPermission.Allowed,
                PolicyPermission.Denied, PolicyPermission.Allowed, 24, "SAME_PRODUCT_ONLY", "RAKUTEN",
                "Controlled disclosure", 0, "Controlled test", now, PolicyPermission.Allowed)
            : await db.MerchantPolicies.FirstAsync(x => x.SourceKey == "demo-fixture");
        var product = Product.Create($"affiliate-product-{suffix}", $"Affiliate Product {suffix}", brand, category,
            $"MODEL-{suffix}", $"MPN-{suffix}", null, new Dictionary<string, string>());
        db.AddRange(retailer, product);
        if (provider == AffiliateProviderType.Rakuten) db.Add(policy);
        await db.SaveChangesAsync();
        var providerProgramId = provider == AffiliateProviderType.Rakuten ? $"rakuten-{suffix}" : "provider-program";
        var listing = RetailerListing.Create(product.Id, retailer.Id, $"LISTING-{suffix}", product.Title, destinationUrl,
            policy.Id, MatchState.Confirmed, now, now, 99m, "CAD", FreshnessState.Recent, EvidenceState.Strong,
            HistoryAvailability.Unavailable, approvedAffiliateDestinationReference: destinationUrl);
        var program = AffiliateProgram.Create(retailer.Id, provider, status == AffiliateProgramStatus.Active ? AffiliateProgramStatus.Active : AffiliateProgramStatus.PendingApproval,
            now, providerProgramId, "media-property", "provider-link", true,
            destinationDomains ?? ["merchant.safe.test"], trackingDomains ?? ["tracking.safe.test"], "controlled-test-evidence", now);
        if (status != AffiliateProgramStatus.Active) program.SetStatus(status, now);
        db.AddRange(listing, program);
        if (provider == AffiliateProviderType.Rakuten)
        {
            var capability = RakutenAdvertiserCapability.Create(providerProgramId, "Controlled Rakuten merchant",
                "https://merchant.safe.test", IntegrationAdvertiserStatus.Active, IntegrationPartnershipStatus.Active,
                ["CA"], true, true, now, now, now);
            capability.ConfigureOperatorMapping(retailer.Id, policy.Id, true, true, true, now);
            db.Add(capability);
        }
        await db.SaveChangesAsync();

        var linkId = Guid.Empty;
        if (createLink)
        {
            var link = AffiliateLink.CreateActive(listing.Id, program.Id, provider, trackingUrl, destinationUrl,
                now, now.AddDays(7), now.AddDays(30), "controlled-test-link");
            db.Add(link);
            await db.SaveChangesAsync();
            linkId = link.Id;
        }
        return (listing.Id, linkId, program.Id, providerProgramId);
    }

    private sealed class DeterministicProvider : IAffiliateLinkProvider
    {
        public AffiliateProviderType Provider => AffiliateProviderType.Impact;
        public Task<AffiliateLinkResolution> ResolveAsync(AffiliateLinkRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AffiliateLinkResolution(AffiliateResolutionStatus.Success, Provider,
                "https://tracking.safe.test/click/refreshed", request.Program.ProviderProgramId,
                request.Listing.ApprovedAffiliateDestinationReference, now, now.AddDays(30), now.AddDays(7), "fake-provider-reference"));
        }
    }

    private sealed class FakePartnershipClient(IReadOnlyList<RakutenPartnershipRecord> rows) : IRakutenPartnershipClient
    {
        public Task<IReadOnlyList<RakutenPartnershipRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(rows);
    }

    private sealed class FakeAdvertiserClient(IReadOnlyList<RakutenAdvertiserRecord> rows) : IRakutenAdvertiserClient
    {
        public Task<IReadOnlyList<RakutenAdvertiserRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(rows);
    }
}
