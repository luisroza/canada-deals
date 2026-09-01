using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed record RakutenImportSummary(
    Guid RunId,
    IntegrationRunStatus Status,
    bool DryRun,
    int Pages,
    int Records,
    int Created,
    int Updated,
    int Observations,
    int Skipped,
    int PolicyBlocked,
    int ReviewCandidates,
    string? FailureReason);

public sealed class RakutenCatalogImportService(
    DealsDbContext db,
    IRakutenProductSearchClient productSearch,
    IOptions<RakutenOptions> options,
    TimeProvider clock)
{
    private readonly RakutenOptions _options = options.Value;

    public async Task<RakutenImportSummary> RunAsync(string advertiserMid, bool dryRun, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var run = RakutenImportRun.Start(advertiserMid, dryRun, now);
        db.RakutenImportRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var pages = 0;
        var records = 0;
        var created = 0;
        var updated = 0;
        var observations = 0;
        var skipped = 0;
        var policyBlocked = 0;
        var reviewCandidates = 0;
        string? failure = null;
        OperationCanceledException? cancellation = null;
        var terminal = IntegrationRunStatus.Succeeded;
        IDbContextTransaction? transaction = null;

        try
        {
            var capability = await db.RakutenAdvertiserCapabilities
                .Include(candidate => candidate.MerchantPolicy)
                .SingleOrDefaultAsync(candidate => candidate.AdvertiserMid == advertiserMid, cancellationToken);
            if (capability is null || !capability.CanProviderEnableCatalog() || capability.CanadaRelevant != true)
                throw new RakutenProviderException(RakutenFailureKind.Authorization, "RAKUTEN_CATALOG_CAPABILITY_BLOCKED");

            if (!dryRun)
            {
                if (!_options.CatalogImportEnabled)
                    throw new RakutenProviderException(RakutenFailureKind.ConfigurationError, "RAKUTEN_CATALOG_IMPORT_DISABLED");
                if (capability.MerchantPolicy is null || !capability.CanPersistCatalog(capability.MerchantPolicy))
                    throw new RakutenProviderException(RakutenFailureKind.Authorization, "RAKUTEN_CATALOG_POLICY_BLOCKED");
                transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }

            for (var pageNumber = 1; pageNumber <= _options.MaximumPagesPerRun; pageNumber++)
            {
                var page = await productSearch.GetPageAsync(advertiserMid, pageNumber, _options.ProductPageSize, cancellationToken);
                pages++;
                records += page.Products.Count;
                foreach (var product in page.Products)
                {
                    var result = await ProcessProductAsync(capability, product, dryRun, cancellationToken);
                    created += result.Created;
                    updated += result.Updated;
                    observations += result.Observation;
                    skipped += result.Skipped;
                    policyBlocked += result.PolicyBlocked;
                    reviewCandidates += result.ReviewCandidate;
                }
                if (page.TotalPages <= pageNumber || page.Products.Count == 0) break;
            }

            if (!dryRun)
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction!.CommitAsync(cancellationToken);
            }
        }
        catch (RakutenProviderException exception)
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            db.RakutenImportRuns.Attach(run);
            terminal = exception.Kind is RakutenFailureKind.Authorization or RakutenFailureKind.ConfigurationError
                ? IntegrationRunStatus.Blocked
                : IntegrationRunStatus.Failed;
            failure = exception.SafeCode;
        }
        catch (HttpRequestException)
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            db.RakutenImportRuns.Attach(run);
            terminal = IntegrationRunStatus.Failed;
            failure = "RAKUTEN_NETWORK_FAILURE";
        }
        catch (OperationCanceledException exception)
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            db.RakutenImportRuns.Attach(run);
            terminal = IntegrationRunStatus.Failed;
            failure = "RAKUTEN_IMPORT_CANCELLED";
            cancellation = exception;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            db.RakutenImportRuns.Attach(run);
            terminal = IntegrationRunStatus.Failed;
            failure = "RAKUTEN_IMPORT_UNEXPECTED_FAILURE";
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }

        run.Complete(terminal, clock.GetUtcNow(), pages, records, created, updated, observations,
            skipped, policyBlocked, reviewCandidates, failure);
        await db.SaveChangesAsync(CancellationToken.None);
        if (cancellation is not null) ExceptionDispatchInfo.Capture(cancellation).Throw();
        cancellationToken.ThrowIfCancellationRequested();
        return new RakutenImportSummary(run.Id, terminal, dryRun, pages, records, created, updated,
            observations, skipped, policyBlocked, reviewCandidates, failure);
    }

    private async Task<RowResult> ProcessProductAsync(
        RakutenAdvertiserCapability capability,
        RakutenProductRecord source,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(source.AdvertiserMid, capability.AdvertiserMid, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(source.SourceListingKey)) return RowResult.Skip();

        var (amount, currency) = source.CurrentPrice();
        if (amount is null || !string.Equals(currency, "CAD", StringComparison.OrdinalIgnoreCase)) return RowResult.Skip();
        var regularPrice = source.SalePrice is > 0 && source.RetailPrice is > 0 && source.RetailPrice > source.SalePrice &&
                           string.Equals(source.SaleCurrency, "CAD", StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(source.RetailCurrency, "CAD", StringComparison.OrdinalIgnoreCase)
            ? source.RetailPrice
            : null;
        var regularPriceEvidence = regularPrice.HasValue
            ? $"rakuten-product-search:{capability.AdvertiserMid}:{source.SourceListingKey}:retailPrice"
            : null;
        if (!TryApprovedProductUrl(capability.AdvertiserUrl, source.LinkUrl, out var productUrl)) return RowResult.Skip();

        if (dryRun) return RowResult.DryRun();
        var policy = capability.MerchantPolicy!;
        if (!capability.CanPersistCatalog(policy)) return RowResult.PolicyBlock();

        var mapping = await db.RakutenSourceMappings
            .Include(candidate => candidate.RetailerListing).ThenInclude(listing => listing.Product)
            .SingleOrDefaultAsync(candidate => candidate.AdvertiserMid == capability.AdvertiserMid &&
                                               candidate.SourceListingKey == source.SourceListingKey, cancellationToken);
        RetailerListing listing;
        var created = 0;
        var updated = 0;
        if (mapping is null)
        {
            if (string.IsNullOrWhiteSpace(source.Upc)) return RowResult.Review();
            var canonical = await db.Products.Where(product => product.Gtin == source.Upc.Trim()).Take(2).ToListAsync(cancellationToken);
            if (canonical.Count != 1) return RowResult.Review();

            var observedAt = clock.GetUtcNow();
            listing = RetailerListing.Create(
                canonical[0].Id, capability.RetailerId!.Value, ExternalListingId(capability.AdvertiserMid, source.SourceListingKey),
                source.ProductName, productUrl!.ToString(), policy.Id, MatchState.AutoMatched, observedAt, clock.GetUtcNow(),
                amount, "CAD", FreshnessState.Recent, EvidenceState.Unknown,
                policy.CanStoreHistory ? HistoryAvailability.Partial : HistoryAvailability.Unavailable,
                externalIdentifiers: new Dictionary<string, string>
                {
                    ["upc"] = source.Upc.Trim(),
                    ["rakutenMid"] = capability.AdvertiserMid,
                    ["rakutenSourceKey"] = source.SourceListingKey
                }, retailerSku: source.Sku, approvedAffiliateDestinationReference: policy.CanUseAffiliateLinks ? productUrl.ToString() : null,
                seller: null, isMarketplaceSeller: null, condition: ProductCondition.Unknown,
                regionAvailabilityContext: "Canada", onlineAvailability: OnlineAvailabilityState.Unknown, shippingContext: null,
                regularPriceAmount: regularPrice, regularPriceObservedAt: regularPrice.HasValue ? observedAt : null,
                regularPriceEvidenceReference: regularPriceEvidence);
            db.RetailerListings.Add(listing);
            db.RakutenSourceMappings.Add(RakutenSourceMapping.Create(capability.AdvertiserMid, source.SourceListingKey, listing.Id, clock.GetUtcNow()));
            created = 1;
        }
        else
        {
            listing = mapping.RetailerListing;
            var sourceUpc = source.Upc?.Trim();
            var listingUpc = listing.Product.Gtin?.Trim();
            var mappedUpc = listing.ExternalIdentifiers.TryGetValue("upc", out var externalUpc) ? externalUpc.Trim() : null;
            if (string.IsNullOrWhiteSpace(sourceUpc) ||
                string.IsNullOrWhiteSpace(listingUpc) ||
                !string.Equals(sourceUpc, listingUpc, StringComparison.Ordinal) ||
                !string.Equals(sourceUpc, mappedUpc, StringComparison.Ordinal) ||
                await db.Products.CountAsync(product => product.Gtin == sourceUpc, cancellationToken) != 1)
                return RowResult.Review();

            var priceChanged = listing.CurrentPriceAmount != amount.Value ||
                               !string.Equals(listing.CurrentPriceCurrency, "CAD", StringComparison.OrdinalIgnoreCase);
            listing.RecordCurrentPrice(amount.Value, "CAD", clock.GetUtcNow(), clock.GetUtcNow());
            listing.SetRegularPrice(regularPrice, "CAD", regularPrice.HasValue ? clock.GetUtcNow() : null, regularPriceEvidence);
            mapping.MarkSeen(clock.GetUtcNow());
            updated = 1;
            if (!priceChanged) return new RowResult(created, updated, 0, 0, 0, 0);
        }

        if (!policy.CanStoreHistory) return new RowResult(created, updated, 0, 0, 1, 0);

        var hash = SourceHash(capability.AdvertiserMid, source.SourceListingKey, amount.Value, "CAD");
        var sourceObservedAt = clock.GetUtcNow();
        db.PriceObservations.Add(PriceObservation.Create(
            listing.Id, amount.Value, "CAD", sourceObservedAt, clock.GetUtcNow(), true, hash));
        return new RowResult(created, updated, 1, 0, 0, 0);
    }

    private static bool TryApprovedProductUrl(string? advertiserUrl, string? productUrl, out Uri? approved)
    {
        approved = null;
        if (!Uri.TryCreate(advertiserUrl, UriKind.Absolute, out var advertiser) || advertiser.Scheme != Uri.UriSchemeHttps ||
            !Uri.TryCreate(productUrl, UriKind.Absolute, out var product) || product.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(product.UserInfo) || !AffiliateUrlPolicy.HostMatches(product.IdnHost, advertiser.IdnHost)) return false;
        approved = product;
        return true;
    }

    private static string ExternalListingId(string mid, string sourceKey)
    {
        var raw = $"rakuten:{mid}:{sourceKey}";
        return raw.Length <= 160 ? raw : $"rakuten:{mid}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()}";
    }

    private static string SourceHash(string mid, string key, decimal amount, string currency)
    {
        var raw = string.Join('|', "rakuten-v1", mid, key, amount.ToString("0.00", CultureInfo.InvariantCulture), currency);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    private sealed record RowResult(int Created, int Updated, int Observation, int Skipped, int PolicyBlocked, int ReviewCandidate)
    {
        public static RowResult Skip() => new(0, 0, 0, 1, 0, 0);
        public static RowResult PolicyBlock() => new(0, 0, 0, 0, 1, 0);
        public static RowResult Review() => new(0, 0, 0, 0, 0, 1);
        public static RowResult DryRun() => new(0, 0, 0, 0, 0, 0);
    }
}
