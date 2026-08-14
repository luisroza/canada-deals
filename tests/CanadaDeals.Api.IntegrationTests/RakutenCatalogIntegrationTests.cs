using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Matching;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Infrastructure.Rakuten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class RakutenCatalogIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [RequiresPostgresFact]
    public async Task Dry_run_fetches_and_normalizes_without_catalog_mutation()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "dry");
        var service = Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD")]));

        var result = await service.RunAsync(scenario.Mid, true);

        Assert.Equal(IntegrationRunStatus.Succeeded, result.Status);
        Assert.True(result.DryRun);
        Assert.Equal(1, result.Records);
        Assert.False(await db.RakutenSourceMappings.AnyAsync(mapping => mapping.AdvertiserMid == scenario.Mid));
        Assert.False(await db.RetailerListings.AnyAsync(listing => listing.RetailerId == scenario.RetailerId));
        Assert.False(await db.PriceObservations.AnyAsync(observation =>
            db.RetailerListings.Any(listing => listing.Id == observation.RetailerListingId && listing.RetailerId == scenario.RetailerId)));
    }

    [RequiresPostgresFact]
    public async Task Strong_upc_import_is_idempotent_and_price_change_creates_one_new_observation()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "upsert");
        var importStartedAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        var first = Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD")]));

        var firstResult = await first.RunAsync(scenario.Mid, false);
        var repeatResult = await first.RunAsync(scenario.Mid, false);
        var changed = await Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 99.99m, "CAD")])).RunAsync(scenario.Mid, false);
        var reverted = await Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD")])).RunAsync(scenario.Mid, false);

        Assert.Equal(1, firstResult.Created);
        Assert.Equal(1, firstResult.Observations);
        Assert.Equal(1, repeatResult.Updated);
        Assert.Equal(0, repeatResult.Observations);
        Assert.Equal(1, changed.Observations);
        Assert.Equal(1, reverted.Observations);
        var mapping = await db.RakutenSourceMappings.SingleAsync(candidate => candidate.AdvertiserMid == scenario.Mid);
        var listing = await db.RetailerListings.SingleAsync(candidate => candidate.Id == mapping.RetailerListingId);
        Assert.Equal(109.99m, listing.CurrentPriceAmount);
        Assert.Equal(ProductCondition.Unknown, listing.Condition);
        Assert.Null(listing.Seller);
        Assert.Null(listing.IsMarketplaceSeller);
        Assert.True(listing.SourceObservedAt >= importStartedAt);
        Assert.False(MatchingRules.Determine(null, null, null, null, true) == MatchState.AutoMatched);
        Assert.Equal(3, await db.PriceObservations.CountAsync(observation => observation.RetailerListingId == listing.Id));
    }

    [RequiresPostgresFact]
    public async Task Existing_mapping_revalidates_upc_before_mutating_price()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "mapped-upc");
        await Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD")])).RunAsync(scenario.Mid, false);

        var result = await Service(db, new FakeProductClient([Product(scenario.Mid, $"9{scenario.Gtin[1..]}", 1.99m, "CAD")]))
            .RunAsync(scenario.Mid, false);

        Assert.Equal(1, result.ReviewCandidates);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Observations);
        var mapping = await db.RakutenSourceMappings.SingleAsync(candidate => candidate.AdvertiserMid == scenario.Mid);
        var listing = await db.RetailerListings.SingleAsync(candidate => candidate.Id == mapping.RetailerListingId);
        Assert.Equal(109.99m, listing.CurrentPriceAmount);
        Assert.Single(await db.PriceObservations.Where(observation => observation.RetailerListingId == listing.Id).ToListAsync());
    }

    [RequiresPostgresFact]
    public async Task Unknown_price_history_keeps_current_price_but_blocks_observations()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "history-policy", PolicyPermission.Unknown);

        var result = await Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD")])).RunAsync(scenario.Mid, false);

        Assert.Equal(IntegrationRunStatus.Succeeded, result.Status);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.PolicyBlocked);
        Assert.Equal(0, result.Observations);
        var mapping = await db.RakutenSourceMappings.SingleAsync(candidate => candidate.AdvertiserMid == scenario.Mid);
        var listing = await db.RetailerListings.SingleAsync(candidate => candidate.Id == mapping.RetailerListingId);
        Assert.Equal(109.99m, listing.CurrentPriceAmount);
        Assert.Equal(HistoryAvailability.Unavailable, listing.History);
        Assert.False(await db.PriceObservations.AnyAsync(observation => observation.RetailerListingId == listing.Id));
    }

    [RequiresPostgresFact]
    public async Task Unknown_policy_blocks_live_write_before_product_fetch()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Unknown, PolicyPermission.Unknown, "policy");
        var client = new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 10m, "CAD")]);

        var result = await Service(db, client).RunAsync(scenario.Mid, false);

        Assert.Equal(IntegrationRunStatus.Blocked, result.Status);
        Assert.Equal("RAKUTEN_CATALOG_POLICY_BLOCKED", result.FailureReason);
        Assert.Equal(0, client.Calls);
        Assert.False(await db.RakutenSourceMappings.AnyAsync(mapping => mapping.AdvertiserMid == scenario.Mid));
    }

    [RequiresPostgresFact]
    public async Task Unsupported_currency_and_weak_identity_do_not_publish()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "safety");
        var records = new[]
        {
            Product(scenario.Mid, scenario.Gtin, 10m, "USD", "usd-link"),
            Product(scenario.Mid, null, 20m, "CAD", "weak-link")
        };

        var result = await Service(db, new FakeProductClient(records)).RunAsync(scenario.Mid, false);

        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, result.ReviewCandidates);
        Assert.False(await db.RakutenSourceMappings.AnyAsync(mapping => mapping.AdvertiserMid == scenario.Mid));
    }

    [RequiresPostgresFact]
    public async Task Conflicting_identifier_is_quarantined_instead_of_merged()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "conflict");
        var brand = await db.Brands.FirstAsync();
        var category = await db.Categories.FirstAsync();
        db.Products.Add(CanadaDeals.Domain.Catalog.Product.Create($"conflict-{Guid.NewGuid():N}", "Conflicting Product", brand, category, gtin: scenario.Gtin));
        await db.SaveChangesAsync();

        var result = await Service(db, new FakeProductClient([Product(scenario.Mid, scenario.Gtin, 10m, "CAD")])).RunAsync(scenario.Mid, false);

        Assert.Equal(1, result.ReviewCandidates);
        Assert.False(await db.RakutenSourceMappings.AnyAsync(mapping => mapping.AdvertiserMid == scenario.Mid));
    }

    [RequiresPostgresFact]
    public async Task Unexpected_provider_failure_closes_the_import_audit_without_leaking_exception_detail()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "unexpected");

        var result = await Service(db, new ThrowingProductClient()).RunAsync(scenario.Mid, false);

        Assert.Equal(IntegrationRunStatus.Failed, result.Status);
        Assert.Equal("RAKUTEN_IMPORT_UNEXPECTED_FAILURE", result.FailureReason);
        var audit = await db.RakutenImportRuns.SingleAsync(run => run.Id == result.RunId);
        Assert.Equal(IntegrationRunStatus.Failed, audit.Status);
        Assert.NotNull(audit.FinishedAt);
        Assert.DoesNotContain("controlled-sensitive-detail", audit.FailureReason);
    }

    [RequiresPostgresFact]
    public async Task Partial_failure_rolls_back_catalog_and_a_retry_succeeds_once()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "rollback-retry");
        var product = Product(scenario.Mid, scenario.Gtin, 109.99m, "CAD");

        var failed = await Service(db, new FailOnSecondPageClient(product)).RunAsync(scenario.Mid, false);

        Assert.Equal(IntegrationRunStatus.Failed, failed.Status);
        Assert.False(await db.RakutenSourceMappings.AnyAsync(mapping => mapping.AdvertiserMid == scenario.Mid));
        Assert.False(await db.RetailerListings.AnyAsync(listing => listing.RetailerId == scenario.RetailerId));

        var retried = await Service(db, new FakeProductClient([product])).RunAsync(scenario.Mid, false);

        Assert.Equal(IntegrationRunStatus.Succeeded, retried.Status);
        Assert.Equal(1, retried.Created);
        Assert.Equal(1, retried.Observations);
        Assert.Single(await db.RakutenSourceMappings.Where(mapping => mapping.AdvertiserMid == scenario.Mid).ToListAsync());
    }

    [RequiresPostgresFact]
    public async Task Cancellation_closes_the_import_audit_before_propagating()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var scenario = await CreateScenarioAsync(db, PolicyPermission.Allowed, PolicyPermission.Allowed, "cancel");

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Service(db, new CancellationProductClient()).RunAsync(scenario.Mid, false));

        var audit = await db.RakutenImportRuns
            .Where(run => run.AdvertiserMid == scenario.Mid)
            .OrderByDescending(run => run.StartedAt)
            .FirstAsync();
        Assert.Equal(IntegrationRunStatus.Failed, audit.Status);
        Assert.Equal("RAKUTEN_IMPORT_CANCELLED", audit.FailureReason);
        Assert.NotNull(audit.FinishedAt);
    }

    private static RakutenCatalogImportService Service(DealsDbContext db, IRakutenProductSearchClient client) => new(
        db, client, Options.Create(new RakutenOptions
        {
            Enabled = true,
            CatalogImportEnabled = true,
            ProductPageSize = 20,
            MaximumPagesPerRun = 2
        }), TimeProvider.System);

    private static async Task<(string Mid, string Gtin, Guid RetailerId)> CreateScenarioAsync(
        DealsDbContext db,
        PolicyPermission price,
        PolicyPermission metadata,
        string label,
        PolicyPermission history = PolicyPermission.Allowed)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var mid = Random.Shared.Next(100000, 999999).ToString();
        var gtin = $"8{Random.Shared.NextInt64(10000000000, 99999999999)}";
        var now = DateTimeOffset.UtcNow;
        var retailer = Retailer.Create($"rakuten-{label}-{suffix}", $"Rakuten {label} {suffix}");
        var policy = MerchantPolicy.Create($"rakuten-{label}-{suffix}", price, history,
            PolicyPermission.Denied, metadata, 24, "SAME_PRODUCT_ONLY", "RAKUTEN", "Affiliate disclosure",
            0, "External provider", now, PolicyPermission.Allowed);
        var brand = await db.Brands.FirstAsync();
        var category = await db.Categories.FirstAsync();
        var product = CanadaDeals.Domain.Catalog.Product.Create($"rakuten-{label}-{suffix}", $"Rakuten Controlled {label} {suffix}", brand, category, gtin: gtin);
        var capability = RakutenAdvertiserCapability.Create(mid, retailer.Name, "https://merchant.safe.test",
            IntegrationAdvertiserStatus.Active, IntegrationPartnershipStatus.Active, ["CA"], true, true, now, now, now);
        capability.ConfigureOperatorMapping(retailer.Id, policy.Id, true, true, true, now);
        db.AddRange(retailer, policy, product, capability);
        await db.SaveChangesAsync();
        return (mid, gtin, retailer.Id);
    }

    private static RakutenProductRecord Product(string mid, string? upc, decimal amount, string currency, string linkId = "link-1") =>
        new(mid, "Controlled Merchant", linkId, DateTimeOffset.Parse("2026-08-13T12:30:00Z"), $"SKU-{linkId}",
            $"Controlled Product {linkId}", "Electronics", "Controlled", amount + 10m, currency, amount, currency,
            upc, "Short", "Long", "controlled", $"https://merchant.safe.test/products/{linkId}", "https://images.safe.test/item.jpg");

    private sealed class FakeProductClient(IEnumerable<RakutenProductRecord> products) : IRakutenProductSearchClient
    {
        private readonly IReadOnlyList<RakutenProductRecord> _products = products.ToList();
        public int Calls { get; private set; }
        public Task<RakutenProductPage> GetPageAsync(string advertiserMid, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new RakutenProductPage(_products.Count, 1, 1, _products));
        }
    }

    private sealed class ThrowingProductClient : IRakutenProductSearchClient
    {
        public Task<RakutenProductPage> GetPageAsync(string advertiserMid, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("controlled-sensitive-detail");
    }

    private sealed class FailOnSecondPageClient(RakutenProductRecord product) : IRakutenProductSearchClient
    {
        public Task<RakutenProductPage> GetPageAsync(string advertiserMid, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            pageNumber == 1
                ? Task.FromResult(new RakutenProductPage(1, 2, 1, [product]))
                : throw new HttpRequestException("controlled second-page failure");
    }

    private sealed class CancellationProductClient : IRakutenProductSearchClient
    {
        public Task<RakutenProductPage> GetPageAsync(string advertiserMid, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            throw new OperationCanceledException("controlled cancellation", cancellationToken);
    }
}
