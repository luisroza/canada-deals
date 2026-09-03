using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Domain.Search;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class CatalogImportService(
    DealsDbContext db,
    IEnumerable<IOfferCatalogSource> sources,
    IOptions<CatalogIngestionOptions> options,
    TimeProvider clock)
{
    private readonly CatalogIngestionOptions _options = options.Value;

    public async Task<CatalogImportSummary> RunAsync(
        string provider,
        string providerAdvertiserId,
        string? catalogId,
        bool dryRun,
        string? query = null,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedAdvertiserId = providerAdvertiserId.Trim();
        var normalizedCatalogId = catalogId?.Trim() ?? string.Empty;
        var source = sources.SingleOrDefault(candidate => string.Equals(candidate.Provider, normalizedProvider, StringComparison.OrdinalIgnoreCase));
        var configured = await db.CatalogMerchantSources
            .Include(candidate => candidate.MerchantPolicy)
            .Include(candidate => candidate.DefaultCategory)
            .SingleOrDefaultAsync(candidate => candidate.Provider == normalizedProvider &&
                candidate.ProviderAdvertiserId == normalizedAdvertiserId &&
                candidate.CatalogId == normalizedCatalogId, cancellationToken);

        var run = CatalogImportRun.Start(normalizedProvider, normalizedAdvertiserId, configured?.RetailerId, dryRun, clock.GetUtcNow());
        db.CatalogImportRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var stats = new ImportStats();
        var status = IntegrationRunStatus.Succeeded;
        string? failure = null;
        OperationCanceledException? cancellation = null;
        IDbContextTransaction? transaction = null;

        try
        {
            if (source is null) throw new CatalogProviderException(CatalogFailureKind.Configuration, "CATALOG_PROVIDER_NOT_REGISTERED");
            if (configured is null) throw new CatalogProviderException(CatalogFailureKind.Configuration, "CATALOG_SOURCE_NOT_MAPPED");
            if (!configured.ProviderAllowsDryRun()) throw new CatalogProviderException(CatalogFailureKind.RelationshipDenied, "CATALOG_SOURCE_DISCOVERY_GATE_BLOCKED");
            if (!dryRun)
            {
                if (!_options.PersistenceEnabled) throw new CatalogProviderException(CatalogFailureKind.Configuration, "CATALOG_PERSISTENCE_DISABLED");
                if (configured.MerchantPolicy is null || !configured.CanPersist(configured.MerchantPolicy))
                    throw new CatalogProviderException(CatalogFailureKind.Authorization, "CATALOG_MERCHANT_POLICY_BLOCKED");
                transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }

            string? cursor = null;
            for (var pageNumber = 1; pageNumber <= _options.MaximumPagesPerRun && stats.Records < _options.MaximumRecordsPerRun; pageNumber++)
            {
                var remaining = _options.MaximumRecordsPerRun - stats.Records;
                var page = await source.FetchOffersAsync(new CatalogRequest(
                    normalizedAdvertiserId, normalizedCatalogId.Length == 0 ? null : normalizedCatalogId, query, categoryId, pageNumber,
                    Math.Min(_options.PageSize, remaining), remaining, cursor), cancellationToken);
                stats.Pages++;
                foreach (var offer in page.Offers.Take(remaining))
                {
                    stats.Records++;
                    var row = await ProcessOfferAsync(configured, offer, dryRun, cancellationToken);
                    stats.Add(row);
                }
                cursor = page.NextCursor;
                if (!page.HasMore || page.Offers.Count == 0) break;
            }

            if (!dryRun)
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction!.CommitAsync(cancellationToken);
            }
        }
        catch (CatalogProviderException exception)
        {
            await RollbackAsync(transaction);
            Reattach(run);
            status = exception.Kind is CatalogFailureKind.Configuration or CatalogFailureKind.Authentication or
                CatalogFailureKind.Authorization or CatalogFailureKind.RelationshipDenied or CatalogFailureKind.InvalidRequest
                ? IntegrationRunStatus.Blocked : IntegrationRunStatus.Failed;
            failure = exception.SafeCode;
        }
        catch (OperationCanceledException exception)
        {
            await RollbackAsync(transaction);
            Reattach(run);
            status = IntegrationRunStatus.Failed;
            failure = "CATALOG_IMPORT_CANCELLED";
            cancellation = exception;
        }
        catch (HttpRequestException)
        {
            await RollbackAsync(transaction);
            Reattach(run);
            status = IntegrationRunStatus.Failed;
            failure = "CATALOG_NETWORK_FAILURE";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RollbackAsync(transaction);
            Reattach(run);
            status = IntegrationRunStatus.Failed;
            failure = "CATALOG_IMPORT_UNEXPECTED_FAILURE";
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }

        run.Complete(status, clock.GetUtcNow(), stats.Pages, stats.Records, stats.Valid, stats.Cad,
            stats.Mapped, stats.Unmapped, stats.Created, stats.Updated, stats.Observations, stats.Skipped,
            stats.PolicyBlocked, stats.Review, stats.UnsupportedCurrency, stats.Invalid, failure);
        await db.SaveChangesAsync(CancellationToken.None);
        if (cancellation is not null) ExceptionDispatchInfo.Capture(cancellation).Throw();
        cancellationToken.ThrowIfCancellationRequested();
        return new CatalogImportSummary(run.Id, normalizedProvider, normalizedAdvertiserId, status, dryRun,
            stats.Pages, stats.Records, stats.Valid, stats.Cad, stats.Mapped, stats.Unmapped,
            stats.Created, stats.Updated, stats.Observations, stats.Skipped, stats.PolicyBlocked,
            stats.Review, stats.UnsupportedCurrency, stats.Invalid, failure);
    }

    private async Task<RowResult> ProcessOfferAsync(
        CatalogMerchantSource configured,
        ExternalOffer offer,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (!ValidOffer(configured, offer, out var destination)) return RowResult.InvalidRow();
        if (!string.Equals(offer.Currency, "CAD", StringComparison.OrdinalIgnoreCase)) return RowResult.Unsupported();
        if (configured.MerchantPolicy is null || configured.DefaultCategory is null || !configured.RetailerId.HasValue)
            return RowResult.PolicyBlock();

        var mapping = await db.CatalogSourceMappings
            .Include(candidate => candidate.RetailerListing).ThenInclude(listing => listing.Product).ThenInclude(product => product.Brand)
            .SingleOrDefaultAsync(candidate => candidate.Provider == configured.Provider &&
                candidate.ProviderAdvertiserId == configured.ProviderAdvertiserId &&
                candidate.SourceListingKey == offer.SourceListingKey, cancellationToken);

        if (dryRun)
        {
            if (mapping is not null) return RowResult.ValidMapped();
            var canonical = await FindCanonicalProductsAsync(offer, cancellationToken);
            return canonical.Count == 1 || CanCreateCanonical(offer)
                ? RowResult.ValidUnmapped()
                : RowResult.ReviewRow();
        }

        if (!configured.CanPersist(configured.MerchantPolicy)) return RowResult.PolicyBlock();

        RetailerListing listing;
        var created = 0;
        var updated = 0;
        if (mapping is null)
        {
            var canonical = await FindCanonicalProductsAsync(offer, cancellationToken);
            Product product;
            if (canonical.Count > 1) return RowResult.ReviewRow();
            if (canonical.Count == 1) product = canonical[0];
            else
            {
                if (!CanCreateCanonical(offer)) return RowResult.ReviewRow();
                var brand = await ResolveOrCreateBrandAsync(offer.Brand!, cancellationToken);
                product = Product.Create(ProductSlug(configured, offer), offer.Title.Trim(), brand,
                    configured.DefaultCategory, offer.Model, offer.Mpn, StrongGtin(offer));
                db.Products.Add(product);
            }

            var observedAt = offer.SourceUpdatedAt ?? offer.FetchedAt;
            var regularPrice = SupportedRegularPrice(offer);
            listing = RetailerListing.Create(product.Id, configured.RetailerId.Value,
                ExternalListingId(configured, offer), offer.OriginalTitle.Trim(), destination!.ToString(),
                configured.MerchantPolicy.Id, MatchState.AutoMatched, observedAt, offer.FetchedAt,
                offer.CurrentPrice, "CAD", FreshnessState.Recent, EvidenceState.Partial,
                configured.MerchantPolicy.CanStoreHistory ? HistoryAvailability.Partial : HistoryAvailability.Unavailable,
                externalIdentifiers: ExternalIdentifiers(configured, offer), retailerSku: offer.Sku,
                seller: offer.Seller, isMarketplaceSeller: offer.Marketplace, condition: offer.Condition,
                regionAvailabilityContext: offer.Region ?? "Canada", onlineAvailability: offer.Availability,
                shippingContext: offer.Shipping, regularPriceAmount: regularPrice,
                regularPriceObservedAt: regularPrice.HasValue ? observedAt : null,
                regularPriceEvidenceReference: regularPrice.HasValue ? PriceEvidence(configured, offer) : null,
                offerValidFrom: offer.PromotionStart, offerValidUntil: offer.PromotionEnd);
            db.RetailerListings.Add(listing);
            db.CatalogSourceMappings.Add(CatalogSourceMapping.Create(configured.Provider,
                configured.ProviderAdvertiserId, offer.SourceListingKey, listing.Id, offer.FetchedAt));
            created = 1;
        }
        else
        {
            listing = mapping.RetailerListing;
            if (!IdentityStillMatches(listing.Product, offer)) return RowResult.ReviewRow();
            var priceChanged = listing.CurrentPriceAmount != offer.CurrentPrice ||
                !string.Equals(listing.CurrentPriceCurrency, "CAD", StringComparison.OrdinalIgnoreCase);
            var observedAt = offer.SourceUpdatedAt ?? offer.FetchedAt;
            var regularPrice = SupportedRegularPrice(offer);
            listing.RefreshFromCatalog(offer.OriginalTitle, destination!.ToString(), offer.Sku,
                offer.Seller, offer.Marketplace, offer.Condition, offer.Availability, offer.Region ?? "Canada",
                offer.Shipping, ExternalIdentifiers(configured, offer), offer.CurrentPrice!.Value, "CAD",
                observedAt, offer.FetchedAt, regularPrice,
                regularPrice.HasValue ? PriceEvidence(configured, offer) : null,
                offer.PromotionStart, offer.PromotionEnd);
            mapping.MarkSeen(offer.FetchedAt);
            updated = 1;
            if (!priceChanged) return new RowResult(1, 1, 0, 0, 0, 1, 0, updated, 0, 0, 0, 0, 0);
        }

        if (!configured.MerchantPolicy.CanStoreHistory)
            return new RowResult(1, 1, 0, 0, 0, 1, created, updated, 0, 1, 0, 0, 0);

        db.PriceObservations.Add(PriceObservation.Create(listing.Id, offer.CurrentPrice!.Value, "CAD",
            offer.SourceUpdatedAt ?? offer.FetchedAt, offer.FetchedAt, true, SourceHash(configured, offer)));
        return new RowResult(1, 1, 0, 0, 0, 1, created, updated, 1, 0, 0, 0, 0);
    }

    private bool ValidOffer(CatalogMerchantSource configured, ExternalOffer offer, out Uri? destination)
    {
        destination = null;
        return string.Equals(configured.Provider, offer.Provider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(configured.ProviderAdvertiserId, offer.ProviderAdvertiserId, StringComparison.Ordinal) &&
            LengthAtMost(offer.Provider, 24) && LengthAtMost(offer.ProviderAdvertiserId, 160) &&
            !string.IsNullOrWhiteSpace(offer.ExternalListingId) && offer.ExternalListingId.Trim().Length <= 240 &&
            !string.IsNullOrWhiteSpace(offer.Title) && offer.Title.Trim().Length <= 240 &&
            !string.IsNullOrWhiteSpace(offer.OriginalTitle) && offer.OriginalTitle.Trim().Length <= 300 &&
            LengthAtMost(offer.Brand, 120) && LengthAtMost(offer.Sku, 240) &&
            LengthAtMost(offer.Upc, 32) && LengthAtMost(offer.Gtin, 32) &&
            LengthAtMost(offer.Mpn, 120) && LengthAtMost(offer.Model, 120) &&
            LengthAtMost(offer.DestinationUrl, 1000) && LengthAtMost(offer.ProviderAffiliateUrl, 2000) &&
            LengthAtMost(offer.ImageUrl, 2000) && LengthAtMost(offer.Seller, 240) &&
            LengthAtMost(offer.Region, 240) && LengthAtMost(offer.Shipping, 500) &&
            LengthAtMost(offer.PrimaryCategory, 240) && LengthAtMost(offer.SecondaryCategory, 240) &&
            offer.CurrentPrice is > 0 and <= 1_000_000 && decimal.Round(offer.CurrentPrice.Value, 2) == offer.CurrentPrice &&
            offer.Currency?.Trim().Length == 3 &&
            offer.ProviderMetadata.Count <= _options.MaximumMetadataEntries &&
            offer.ProviderMetadata.All(pair => pair.Key.Length is > 0 and <= 80 && pair.Value.Length <= _options.MaximumMetadataValueLength) &&
            AffiliateUrlPolicy.TryValidateHttps(offer.DestinationUrl, configured.AllowedDestinationHosts, out destination);
    }

    private async Task<List<Product>> FindCanonicalProductsAsync(ExternalOffer offer, CancellationToken cancellationToken)
    {
        var gtin = StrongGtin(offer);
        if (!string.IsNullOrWhiteSpace(gtin))
            return await db.Products.Include(product => product.Brand).Where(product => product.Gtin == gtin).Take(2).ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(offer.Mpn) || string.IsNullOrWhiteSpace(offer.Brand)) return [];
        var mpn = DiscoveryRules.NormalizeIdentifier(offer.Mpn);
        var brand = Brand.NormalizeKey(offer.Brand);
        return await db.Products.Include(product => product.Brand)
            .Where(product => product.NormalizedManufacturerPartNumber == mpn && product.Brand.NormalizedKey == brand)
            .Take(2).ToListAsync(cancellationToken);
    }

    private async Task<Brand> ResolveOrCreateBrandAsync(string name, CancellationToken cancellationToken)
    {
        var key = Brand.NormalizeKey(name);
        var existing = await db.Brands.SingleOrDefaultAsync(brand => brand.NormalizedKey == key, cancellationToken);
        if (existing is not null) return existing;
        var slugBase = string.Join('-', key.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var slug = slugBase.Length > 100 ? slugBase[..100] : slugBase;
        if (await db.Brands.AnyAsync(brand => brand.Slug == slug, cancellationToken))
            slug = $"{slug}-{ShortHash(key)}";
        var created = Brand.Create(name.Trim(), slug);
        db.Brands.Add(created);
        return created;
    }

    private static bool CanCreateCanonical(ExternalOffer offer) =>
        !string.IsNullOrWhiteSpace(offer.Brand) &&
        (!string.IsNullOrWhiteSpace(StrongGtin(offer)) || !string.IsNullOrWhiteSpace(offer.Mpn));

    private static bool IdentityStillMatches(Product product, ExternalOffer offer)
    {
        var gtin = StrongGtin(offer);
        if (!string.IsNullOrWhiteSpace(gtin)) return string.Equals(product.Gtin, gtin, StringComparison.Ordinal);
        return !string.IsNullOrWhiteSpace(offer.Mpn) && !string.IsNullOrWhiteSpace(offer.Brand) &&
            string.Equals(product.NormalizedManufacturerPartNumber, DiscoveryRules.NormalizeIdentifier(offer.Mpn), StringComparison.Ordinal) &&
            string.Equals(product.Brand.NormalizedKey, Brand.NormalizeKey(offer.Brand), StringComparison.Ordinal);
    }

    private static decimal? SupportedRegularPrice(ExternalOffer offer) =>
        offer.RegularPrice is > 0 and <= 1_000_000 && offer.CurrentPrice is > 0 && offer.RegularPrice > offer.CurrentPrice &&
        decimal.Round(offer.RegularPrice.Value, 2) == offer.RegularPrice
            ? offer.RegularPrice : null;

    private static string? StrongGtin(ExternalOffer offer) => NormalizeGtin(offer.Gtin) ?? NormalizeGtin(offer.Upc);

    private static string? NormalizeGtin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var candidate = value.Trim();
        if (candidate.Length is not (8 or 12 or 13 or 14) || candidate.Any(character => !char.IsAsciiDigit(character)) ||
            candidate.All(character => character == '0')) return null;
        var sum = 0;
        var weight = 3;
        for (var index = candidate.Length - 2; index >= 0; index--)
        {
            sum += (candidate[index] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }
        var checkDigit = (10 - sum % 10) % 10;
        return candidate[^1] - '0' == checkDigit ? candidate : null;
    }

    private static bool LengthAtMost(string? value, int maximum) => value is null || value.Trim().Length <= maximum;

    private static IReadOnlyDictionary<string, string> ExternalIdentifiers(CatalogMerchantSource configured, ExternalOffer offer)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = configured.Provider,
            ["providerAdvertiserId"] = configured.ProviderAdvertiserId,
            ["sourceListingKey"] = offer.SourceListingKey
        };
        Add(values, "gtin", StrongGtin(offer));
        Add(values, "mpn", offer.Mpn);
        Add(values, "sku", offer.Sku);
        return values;
    }

    private static void Add(IDictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim();
    }

    private static string ExternalListingId(CatalogMerchantSource configured, ExternalOffer offer)
    {
        var raw = $"{configured.Provider}:{configured.ProviderAdvertiserId}:{offer.SourceListingKey}";
        return raw.Length <= 160 ? raw : $"{configured.Provider}:{ShortHash(raw, 48)}";
    }

    private static string ProductSlug(CatalogMerchantSource configured, ExternalOffer offer) =>
        $"catalog-{configured.Provider}-{ShortHash($"{configured.ProviderAdvertiserId}:{offer.SourceListingKey}", 24)}";

    private static string PriceEvidence(CatalogMerchantSource configured, ExternalOffer offer) =>
        $"{configured.Provider}:{configured.ProviderAdvertiserId}:{offer.SourceListingKey}:regular-price";

    private static string SourceHash(CatalogMerchantSource configured, ExternalOffer offer)
    {
        var raw = string.Join('|', "catalog-v1", configured.Provider, configured.ProviderAdvertiserId,
            offer.SourceListingKey, offer.CurrentPrice!.Value.ToString("0.00", CultureInfo.InvariantCulture), "CAD");
        return ShortHash(raw, 64);
    }

    private static string ShortHash(string value, int length = 12) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..length];

    private async Task RollbackAsync(IDbContextTransaction? transaction)
    {
        if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
    }

    private void Reattach(CatalogImportRun run)
    {
        db.ChangeTracker.Clear();
        db.CatalogImportRuns.Attach(run);
    }

    private sealed record RowResult(
        int Valid, int Cad, int UnsupportedCurrency, int Invalid, int Mapped, int Unmapped,
        int Created, int Updated, int Observations, int Skipped, int PolicyBlocked, int Review, int Reserved)
    {
        public static RowResult InvalidRow() => new(0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static RowResult Unsupported() => new(1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static RowResult PolicyBlock() => new(1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
        public static RowResult ReviewRow() => new(1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0);
        public static RowResult ValidMapped() => new(1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0);
        public static RowResult ValidUnmapped() => new(1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed class ImportStats
    {
        public int Pages, Records, Valid, Cad, Mapped, Unmapped, Created, Updated, Observations,
            Skipped, PolicyBlocked, Review, UnsupportedCurrency, Invalid;

        public void Add(RowResult row)
        {
            Valid += row.Valid; Cad += row.Cad; Mapped += row.Mapped; Unmapped += row.Unmapped;
            Created += row.Created; Updated += row.Updated; Observations += row.Observations;
            Skipped += row.Skipped; PolicyBlocked += row.PolicyBlocked; Review += row.Review;
            UnsupportedCurrency += row.UnsupportedCurrency; Invalid += row.Invalid;
        }
    }
}
