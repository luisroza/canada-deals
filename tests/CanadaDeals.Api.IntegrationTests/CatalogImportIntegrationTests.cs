using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Catalogs;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class CatalogImportIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [RequiresPostgresFact]
    public async Task Discovery_rejects_provider_identity_mismatch_without_persisting_a_snapshot()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var before = await db.CatalogMerchantSources.CountAsync();
        var service = new CatalogDiscoveryService(db, [new MismatchedDiscoverySource()], TimeProvider.System);

        var error = await Assert.ThrowsAsync<CatalogProviderException>(() =>
            service.DiscoverAsync(CatalogProviderNames.Impact, true));

        Assert.Equal("CATALOG_DISCOVERY_PROVIDER_MISMATCH", error.SafeCode);
        Assert.Equal(before, await db.CatalogMerchantSources.CountAsync());
    }

    [RequiresPostgresFact]
    public async Task Dry_run_classifies_without_mutating_catalog_then_live_import_is_idempotent()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, "dry-live");
        var source = new FixtureCatalogSource(Offer(scenario.AdvertiserId, 99.99m, 129.99m));
        var service = Service(db, source);

        var dry = await service.RunAsync(source.Provider, scenario.AdvertiserId, scenario.CatalogId, true);

        Assert.Equal(IntegrationRunStatus.Succeeded, dry.Status);
        Assert.Equal(1, dry.Valid);
        Assert.Equal(1, dry.Cad);
        Assert.Equal(1, dry.Unmapped);
        Assert.False(await db.CatalogSourceMappings.AnyAsync(mapping => mapping.ProviderAdvertiserId == scenario.AdvertiserId));
        Assert.False(await db.RetailerListings.AnyAsync(listing => listing.RetailerId == scenario.RetailerId));

        var first = await service.RunAsync(source.Provider, scenario.AdvertiserId, scenario.CatalogId, false);
        var repeat = await service.RunAsync(source.Provider, scenario.AdvertiserId, scenario.CatalogId, false);

        Assert.Equal(1, first.Created);
        Assert.Equal(1, first.Observations);
        Assert.Equal(1, repeat.Updated);
        Assert.Equal(0, repeat.Observations);
        var mapping = await db.CatalogSourceMappings.SingleAsync(candidate => candidate.ProviderAdvertiserId == scenario.AdvertiserId);
        var listing = await db.RetailerListings.Include(candidate => candidate.Product).SingleAsync(candidate => candidate.Id == mapping.RetailerListingId);
        Assert.Equal(99.99m, listing.CurrentPriceAmount);
        Assert.Equal(129.99m, listing.RegularPriceAmount);
        Assert.Equal("CAD", listing.RegularPriceCurrency);
        Assert.Equal("00012345678936", listing.Product.Gtin);
        Assert.Equal("Northstar", (await db.Brands.SingleAsync(brand => brand.Id == listing.Product.BrandId)).Name);
        Assert.Single(await db.PriceObservations.Where(observation => observation.RetailerListingId == listing.Id).ToListAsync());
        Assert.False(await db.ProductImages.AnyAsync(image => image.ProductId == listing.ProductId));
    }

    [RequiresPostgresFact]
    public async Task Price_change_updates_same_listing_and_creates_only_same_listing_evidence()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, "price-change");
        await Service(db, new FixtureCatalogSource(Offer(scenario.AdvertiserId, 99.99m, 129.99m)))
            .RunAsync(CatalogProviderNames.Impact, scenario.AdvertiserId, scenario.CatalogId, false);

        var changed = await Service(db, new FixtureCatalogSource(Offer(scenario.AdvertiserId, 89.99m, 129.99m)))
            .RunAsync(CatalogProviderNames.Impact, scenario.AdvertiserId, scenario.CatalogId, false);

        Assert.Equal(1, changed.Updated);
        Assert.Equal(1, changed.Observations);
        var mapping = await db.CatalogSourceMappings.SingleAsync(candidate => candidate.ProviderAdvertiserId == scenario.AdvertiserId);
        var listing = await db.RetailerListings.SingleAsync(candidate => candidate.Id == mapping.RetailerListingId);
        Assert.Equal(89.99m, listing.CurrentPriceAmount);
        Assert.Equal(2, await db.PriceObservations.CountAsync(observation => observation.RetailerListingId == listing.Id));
        Assert.Single(await db.CatalogSourceMappings.Where(candidate => candidate.ProviderAdvertiserId == scenario.AdvertiserId).ToListAsync());
    }

    [RequiresPostgresFact]
    public async Task Unsupported_currency_and_unknown_policy_fail_closed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var currencyScenario = await CreateScenarioAsync(db, "usd");
        var usd = Offer(currencyScenario.AdvertiserId, 10m, null) with { Currency = "USD" };

        var dry = await Service(db, new FixtureCatalogSource(usd)).RunAsync(
            CatalogProviderNames.Impact, currencyScenario.AdvertiserId, currencyScenario.CatalogId, true);

        Assert.Equal(1, dry.UnsupportedCurrency);
        Assert.False(await db.CatalogSourceMappings.AnyAsync(candidate => candidate.ProviderAdvertiserId == currencyScenario.AdvertiserId));

        var blockedScenario = await CreateScenarioAsync(db, "unknown-policy", PolicyPermission.Unknown);
        var source = new FixtureCatalogSource(Offer(blockedScenario.AdvertiserId, 10m, null));
        var blocked = await Service(db, source).RunAsync(CatalogProviderNames.Impact,
            blockedScenario.AdvertiserId, blockedScenario.CatalogId, false);

        Assert.Equal(IntegrationRunStatus.Blocked, blocked.Status);
        Assert.Equal("CATALOG_MERCHANT_POLICY_BLOCKED", blocked.FailureReason);
        Assert.Equal(0, source.FetchCalls);
    }

    [RequiresPostgresFact]
    public async Task Weak_identity_routes_to_review_instead_of_merging_by_title()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, "weak");
        var weak = Offer(scenario.AdvertiserId, 10m, null) with
        {
            Gtin = "not-a-valid-gtin", Upc = "123", Mpn = null
        };

        var result = await Service(db, new FixtureCatalogSource(weak)).RunAsync(
            CatalogProviderNames.Impact, scenario.AdvertiserId, scenario.CatalogId, false);

        Assert.Equal(1, result.ReviewCandidates);
        Assert.Equal(0, result.Created);
        Assert.False(await db.CatalogSourceMappings.AnyAsync(candidate => candidate.ProviderAdvertiserId == scenario.AdvertiserId));
    }

    [RequiresPostgresFact]
    public async Task Partial_provider_failure_rolls_back_catalog_but_closes_provider_neutral_audit()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, "rollback");
        var source = new FixtureCatalogSource(Offer(scenario.AdvertiserId, 10m, null), failSecondPage: true);

        var result = await Service(db, source, maximumPages: 2).RunAsync(
            CatalogProviderNames.Impact, scenario.AdvertiserId, scenario.CatalogId, false);

        Assert.Equal(IntegrationRunStatus.Failed, result.Status);
        Assert.Equal("CONTROLLED_PROVIDER_FAILURE", result.FailureReason);
        Assert.False(await db.CatalogSourceMappings.AnyAsync(candidate => candidate.ProviderAdvertiserId == scenario.AdvertiserId));
        Assert.False(await db.RetailerListings.AnyAsync(listing => listing.RetailerId == scenario.RetailerId));
        var audit = await db.CatalogImportRuns.SingleAsync(run => run.Id == result.RunId);
        Assert.NotNull(audit.FinishedAt);
        Assert.Equal(IntegrationRunStatus.Failed, audit.Status);
    }

    private static CatalogImportService Service(DealsDbContext db, IOfferCatalogSource source, int maximumPages = 2) =>
        new(db, [source], Options.Create(new CatalogIngestionOptions
        {
            PersistenceEnabled = true, MaximumPagesPerRun = maximumPages, PageSize = 50,
            MaximumRecordsPerRun = 100, MaximumMetadataEntries = 16, MaximumMetadataValueLength = 240
        }), TimeProvider.System);

    private static async Task<(string AdvertiserId, string CatalogId, Guid RetailerId)> CreateScenarioAsync(
        DealsDbContext db,
        string label,
        PolicyPermission metadata = PolicyPermission.Allowed)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var advertiserId = $"advertiser-{suffix}";
        var catalogId = $"catalog-{suffix}";
        var now = DateTimeOffset.UtcNow;
        var retailer = Retailer.Create($"catalog-{label}-{suffix}", $"Catalog {label} {suffix}");
        var policy = MerchantPolicy.Create($"catalog-{label}-{suffix}", PolicyPermission.Allowed,
            PolicyPermission.Allowed, PolicyPermission.Denied, metadata, 24, "SAME_LISTING_ONLY",
            "PROVIDER", "Commercial disclosure", 0, "Provider controlled", now, PolicyPermission.Unknown);
        var category = await db.Categories.FirstAsync();
        var source = CatalogMerchantSource.CreateDiscovery(CatalogProviderNames.Impact, advertiserId, catalogId,
            retailer.Name, IntegrationPartnershipStatus.Active, true, false, true, "CAD", now);
        source.ConfigureMapping(retailer.Id, policy.Id, category.Id, ["retailer.example"], true, now);
        db.AddRange(retailer, policy, source);
        await db.SaveChangesAsync();
        return (advertiserId, catalogId, retailer.Id);
    }

    private static ExternalOffer Offer(string advertiserId, decimal current, decimal? regular) => new(
        CatalogProviderNames.Impact, advertiserId, null, "source-item-1", "Controlled Catalog Product",
        "Controlled Catalog Product", "Northstar", "SKU-1", null, "00012345678936", "NS-CATALOG-1", "Model 1",
        current, regular, "CAD", null, null, "https://retailer.example/products/source-item-1", null,
        "https://images.example/source-item-1.jpg", ProductCondition.New, null, false,
        OnlineAvailabilityState.Available, "Canada", "Standard", "Electronics", null,
        null, DateTimeOffset.Parse("2026-08-30T12:00:00Z"), DateTimeOffset.UtcNow,
        new Dictionary<string, string> { ["catalogId"] = "controlled" });

    private sealed class FixtureCatalogSource(ExternalOffer offer, bool failSecondPage = false) : IOfferCatalogSource
    {
        public string Provider => CatalogProviderNames.Impact;
        public int FetchCalls { get; private set; }
        public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogCapabilities(true, true, false, true, false, false, 100, "fixture"));
        public Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogCandidate>>([]);
        public Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default)
        {
            FetchCalls++;
            if (failSecondPage && request.PageNumber == 2)
                throw new CatalogProviderException(CatalogFailureKind.ProviderUnavailable, "CONTROLLED_PROVIDER_FAILURE");
            return Task.FromResult(new CatalogPage(request.PageNumber, failSecondPage ? 2 : 1,
                request.PageNumber == 1 ? [offer] : [], failSecondPage && request.PageNumber == 1));
        }
    }

    private sealed class MismatchedDiscoverySource : IOfferCatalogSource
    {
        public string Provider => CatalogProviderNames.Impact;
        public Task<CatalogCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogCapabilities(true, true, false, true, false, false, 100, "fixture"));
        public Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(CatalogDiscoveryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogCandidate>>([
                new(CatalogProviderNames.Cj, "controlled", null, "Controlled", IntegrationPartnershipStatus.Active,
                    true, false, true, "CAD", null, new Dictionary<string, string>())
            ]);
        public Task<CatalogPage> FetchOffersAsync(CatalogRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
