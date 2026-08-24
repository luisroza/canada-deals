using System.Net;
using System.Text.Json;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class StoreAffiliateBannerIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient Client() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    [RequiresPostgresFact]
    public async Task Banner_api_exposes_controlled_paths_without_raw_affiliate_urls_or_commercial_ordering()
    {
        using var response = await Client().GetAsync("/api/v1/store-banners");
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(raw);
        var banners = json.RootElement.EnumerateArray().ToArray();
        var active = banners.Single(item => item.GetProperty("retailerKey").GetString() == "demo-north-electronics");
        var discovery = banners.Single(item => item.GetProperty("retailerKey").GetString() == "demo-home-tool");

        Assert.Equal("ACTIVE_AFFILIATE", active.GetProperty("affiliateStatus").GetString());
        Assert.Equal("/go/store/demo-north-electronics", active.GetProperty("href").GetString());
        Assert.True(active.GetProperty("opensNewTab").GetBoolean());
        Assert.Equal("DISCOVERY_ONLY", discovery.GetProperty("affiliateStatus").GetString());
        Assert.StartsWith("/?retailer=demo-home-tool", discovery.GetProperty("href").GetString());
        Assert.DoesNotContain("https://", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("commission", string.Join(',', banners.Select(item => item.GetProperty("retailerKey").GetString())), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresPostgresFact]
    public async Task Eligible_store_without_an_explicitly_active_banner_profile_is_not_published_in_the_carousel()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var retailer = Retailer.Create($"no-banner-{suffix}", $"No banner {suffix}");
        var brand = await db.Brands.FirstAsync();
        var category = await db.Categories.FirstAsync();
        var policy = await db.MerchantPolicies.FirstAsync(candidate => candidate.SourceKey == "demo-fixture");
        var now = DateTimeOffset.UtcNow;
        var product = Product.Create($"no-banner-product-{suffix}", $"No banner product {suffix}", brand, category,
            $"NB-{suffix}", $"NB-{suffix}", null, new Dictionary<string, string>());
        db.AddRange(retailer, product);
        await db.SaveChangesAsync();
        db.RetailerListings.Add(RetailerListing.Create(product.Id, retailer.Id, $"NB-{suffix}", product.Title,
            "https://merchant.safe.test/no-banner", policy.Id, MatchState.Confirmed, now, now, 20m, "CAD",
            FreshnessState.Recent, EvidenceState.Strong, HistoryAvailability.Unavailable));
        await db.SaveChangesAsync();

        using var response = await Client().GetAsync("/api/v1/store-banners");
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain(retailer.Key, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [RequiresPostgresFact]
    public async Task Active_store_destination_redirects_and_records_exactly_one_minimal_click()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active);

        using var response = await Client().GetAsync($"/go/store/{scenario.RetailerKey}?destination=https://attacker.example");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://tracking.safe.test/store", response.Headers.Location?.ToString());
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var click = await db.ClickEvents.SingleAsync(candidate => candidate.StoreAffiliateDestinationId == scenario.DestinationId);
        Assert.Equal(scenario.RetailerId, click.RetailerId);
        Assert.Equal(scenario.ProgramId, click.AffiliateProgramId);
        Assert.Equal("store_banner", click.Placement);
        Assert.Null(click.AffiliateLinkId);
        Assert.Null(click.RetailerListingId);
    }

    [RequiresPostgresFact]
    public async Task Missing_pending_or_disabled_relationship_fails_closed()
    {
        var missing = await CreateScenarioAsync(AffiliateProgramStatus.PendingApproval, includeProgram: false);
        var pending = await CreateScenarioAsync(AffiliateProgramStatus.PendingApproval);
        var disabled = await CreateScenarioAsync(AffiliateProgramStatus.Active, retailerEnabled: false);

        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync($"/go/store/{missing.RetailerKey}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync($"/go/store/{pending.RetailerKey}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync($"/go/store/{disabled.RetailerKey}")).StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Invalid_tracking_domain_http_destination_and_provider_mismatch_fail_closed()
    {
        var wrongTracking = await CreateScenarioAsync(AffiliateProgramStatus.Active, trackingUrl: "https://attacker.example/store");
        var httpDestination = await CreateScenarioAsync(AffiliateProgramStatus.Active, destinationUrl: "http://merchant.safe.test/");
        var providerMismatch = await CreateScenarioAsync(AffiliateProgramStatus.Active,
            programProvider: AffiliateProviderType.Impact, destinationProvider: AffiliateProviderType.Other);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Client().GetAsync($"/go/store/{wrongTracking.RetailerKey}")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Client().GetAsync($"/go/store/{httpDestination.RetailerKey}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync($"/go/store/{providerMismatch.RetailerKey}")).StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Store_destination_is_unique_per_program_and_program_deletion_is_restricted()
    {
        var scenario = await CreateScenarioAsync(AffiliateProgramStatus.Active);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        db.StoreAffiliateDestinations.Add(StoreAffiliateDestination.CreateActive(
            scenario.RetailerId, scenario.ProgramId, AffiliateProviderType.Other,
            "https://tracking.safe.test/store-2", "https://merchant.safe.test/", DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        await using var deleteScope = fixture.Services.CreateAsyncScope();
        var deleteDb = deleteScope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var program = await deleteDb.AffiliatePrograms.SingleAsync(candidate => candidate.Id == scenario.ProgramId);
        deleteDb.AffiliatePrograms.Remove(program);
        await Assert.ThrowsAsync<DbUpdateException>(() => deleteDb.SaveChangesAsync());
    }

    private async Task<(Guid RetailerId, string RetailerKey, Guid ProgramId, Guid DestinationId)> CreateScenarioAsync(
        AffiliateProgramStatus status,
        string destinationUrl = "https://merchant.safe.test/",
        string trackingUrl = "https://tracking.safe.test/store",
        bool includeProgram = true,
        bool retailerEnabled = true,
        AffiliateProviderType programProvider = AffiliateProviderType.Other,
        AffiliateProviderType destinationProvider = AffiliateProviderType.Other)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var retailer = Retailer.Create($"store-banner-{suffix}", $"Store banner {suffix}");
        retailer.SetEnabled(retailerEnabled);
        var brand = await db.Brands.FirstAsync();
        var category = await db.Categories.FirstAsync();
        var policy = await db.MerchantPolicies.FirstAsync(candidate => candidate.SourceKey == "demo-fixture");
        var now = DateTimeOffset.UtcNow;
        var product = Product.Create($"store-banner-product-{suffix}", $"Store banner product {suffix}", brand, category,
            $"SB-{suffix}", $"SB-{suffix}", null, new Dictionary<string, string>());
        db.AddRange(retailer, product);
        await db.SaveChangesAsync();
        db.AddRange(
            RetailerListing.Create(product.Id, retailer.Id, $"SB-{suffix}", product.Title, "https://merchant.safe.test/product",
                policy.Id, MatchState.Confirmed, now, now, 50m, "CAD", FreshnessState.Recent, EvidenceState.Strong,
                HistoryAvailability.Unavailable),
            StoreBannerProfile.CreateOriginal(retailer.Id, $"Shop {retailer.Name}", "Browse current offers",
                "/store-banners/marketplace-packages.svg", 100));

        if (!includeProgram)
        {
            await db.SaveChangesAsync();
            return (retailer.Id, retailer.Key, Guid.Empty, Guid.Empty);
        }

        var program = AffiliateProgram.Create(retailer.Id, programProvider, status, now,
            "store-program", "store-property", "store-link", true,
            ["merchant.safe.test"], ["tracking.safe.test"], "controlled-store-evidence", now);
        db.AffiliatePrograms.Add(program);
        await db.SaveChangesAsync();
        var destination = StoreAffiliateDestination.CreateActive(retailer.Id, program.Id, destinationProvider,
            trackingUrl, destinationUrl, now, now.AddDays(1), now.AddDays(7), "controlled-store-link");
        db.StoreAffiliateDestinations.Add(destination);
        await db.SaveChangesAsync();
        return (retailer.Id, retailer.Key, program.Id, destination.Id);
    }
}
