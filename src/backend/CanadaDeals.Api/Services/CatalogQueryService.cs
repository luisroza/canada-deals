using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.PriceTruth;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Domain.Search;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using System.Diagnostics;

namespace CanadaDeals.Api.Services;

public sealed class CatalogQueryService(DealsDbContext db, TimeProvider clock, ILogger<CatalogQueryService> logger)
{
    private const string TestOnlyAttribution = "TEST_ONLY";

    public async Task<IReadOnlyList<KeyValuePair<string, string>>> ValidateDiscoveryRequestAsync(
        DiscoveryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<KeyValuePair<string, string>>();
        var now = clock.GetUtcNow();
        var categoryKeys = DiscoveryQueryRequest.Values(request.Category);
        if (categoryKeys.Length > 0)
        {
            var existing = await db.Categories
                .Where(x => x.IsEnabled && categoryKeys.Contains(x.Slug) && db.RetailerListings.Any(listing =>
                    listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                    listing.Retailer.IsEnabled && listing.Product.Brand.IsEnabled && listing.Product.CategoryId == x.Id &&
                    listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution))
                .Select(x => x.Slug)
                .ToListAsync(cancellationToken);
            foreach (var missing in categoryKeys.Except(existing, StringComparer.OrdinalIgnoreCase))
                errors.Add(new(nameof(request.Category), $"Unknown category '{missing}'."));
        }

        var retailerKeys = DiscoveryQueryRequest.Values(request.Retailer);
        if (retailerKeys.Length > 0)
        {
            var existing = await db.Retailers
                .Where(x => x.IsEnabled && retailerKeys.Contains(x.Key) && db.RetailerListings.Any(listing =>
                    listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                    listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled && listing.RetailerId == x.Id &&
                    listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution))
                .Select(x => x.Key)
                .ToListAsync(cancellationToken);
            foreach (var missing in retailerKeys.Except(existing, StringComparer.OrdinalIgnoreCase))
                errors.Add(new(nameof(request.Retailer), $"Unknown retailer '{missing}'."));
        }

        return errors;
    }

    public async Task<DiscoveryResponse> GetDiscoveryAsync(DiscoveryQueryRequest request, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var now = clock.GetUtcNow();
        var search = request.Search?.Trim() ?? string.Empty;
        var normalizedSearch = DiscoveryRules.NormalizeIdentifier(search);
        var escapedSearch = EscapeLike(search);
        var sort = ParseSort(request.Sort) ?? DiscoveryRules.DefaultSort(search);
        var categoryKeys = DiscoveryQueryRequest.Values(request.Category);
        var retailerKeys = DiscoveryQueryRequest.Values(request.Retailer);
        var freshness = DiscoveryQueryRequest.Values(request.Freshness);
        var matches = DiscoveryQueryRequest.Values(request.Match);
        var availability = DiscoveryQueryRequest.Values(request.Availability);

        var query = db.RetailerListings.AsNoTracking()
            .Where(x => x.IsEnabled && (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) && x.Retailer.IsEnabled &&
                        x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled && x.CurrentPriceAmount != null &&
                        x.CurrentPriceCurrency == PriceAlert.SupportedCurrency &&
                        x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                        x.MerchantPolicy.RequiredAttribution != TestOnlyAttribution);

        if (search.Length == 0 && availability.Length == 0)
            query = query.Where(x => x.OnlineAvailability != OnlineAvailabilityState.Unavailable);

        if (categoryKeys.Length > 0) query = query.Where(x => categoryKeys.Contains(x.Product.Category.Slug));
        if (retailerKeys.Length > 0) query = query.Where(x => retailerKeys.Contains(x.Retailer.Key));

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            query = query.Where(x =>
                (x.MatchState == MatchState.AutoMatched || x.MatchState == MatchState.Confirmed) &&
                x.OnlineAvailability == OnlineAvailabilityState.Available);
            if (request.MinPrice.HasValue) query = query.Where(x => x.CurrentPriceAmount >= request.MinPrice.Value);
            if (request.MaxPrice.HasValue) query = query.Where(x => x.CurrentPriceAmount <= request.MaxPrice.Value);
        }

        if (request.HasReference.HasValue)
        {
            query = query.Where(x =>
                (x.RegularPriceAmount != null && x.RegularPriceAmount > x.CurrentPriceAmount &&
                 x.RegularPriceCurrency == x.CurrentPriceCurrency && x.RegularPriceObservedAt != null &&
                 x.RegularPriceEvidenceReference != null && x.RegularPriceEvidenceReference != "") == request.HasReference.Value);
        }

        if (freshness.Length > 0)
        {
            var recentBoundary = now.AddHours(-6);
            var agingBoundary = now.AddHours(-24);
            query = query.Where(x =>
                (freshness.Contains("recent") && x.SourceObservedAt >= recentBoundary) ||
                (freshness.Contains("aging") && x.SourceObservedAt < recentBoundary && x.SourceObservedAt >= agingBoundary) ||
                (freshness.Contains("stale") && x.SourceObservedAt < agingBoundary) ||
                (freshness.Contains("unknown") && x.SourceObservedAt == null));
        }

        if (matches.Length > 0)
        {
            query = query.Where(x =>
                (matches.Contains("safe") && (x.MatchState == MatchState.AutoMatched || x.MatchState == MatchState.Confirmed)) ||
                (matches.Contains("review") && (x.MatchState == MatchState.PossibleMatchReview || x.MatchState == MatchState.ManualReview)) ||
                (matches.Contains("none") && x.MatchState == MatchState.NoMatch));
        }

        if (availability.Length > 0)
        {
            query = query.Where(x =>
                (availability.Contains("online") && x.OnlineAvailability == OnlineAvailabilityState.Available) ||
                (availability.Contains("unavailable") && x.OnlineAvailability == OnlineAvailabilityState.Unavailable) ||
                (availability.Contains("unknown") && x.OnlineAvailability == OnlineAvailabilityState.Unknown));
        }

        if (search.Length > 0)
        {
            query = query.Where(x =>
                x.Product.NormalizedModelNumber == normalizedSearch ||
                x.Product.NormalizedManufacturerPartNumber == normalizedSearch ||
                x.Product.Gtin == normalizedSearch ||
                EF.Functions.ILike(x.Product.Title, escapedSearch, "\\") ||
                EF.Functions.ILike(x.Product.Title, $"{escapedSearch}%", "\\") ||
                EF.Property<NpgsqlTsVector>(x.Product, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery("english", search)) ||
                EF.Functions.TrigramsAreWordSimilar(search, x.Product.SearchDocument));
        }

        var candidates = query.Select(x => new DiscoveryCandidate
        {
            ListingId = x.Id,
            ProductId = x.ProductId,
            ObservedAt = x.SourceObservedAt,
            CurrentPrice = x.CurrentPriceAmount!.Value,
            ExactModel = search.Length > 0 && x.Product.NormalizedModelNumber == normalizedSearch,
            ExactMpn = search.Length > 0 && x.Product.NormalizedManufacturerPartNumber == normalizedSearch,
            ExactGtin = search.Length > 0 && x.Product.Gtin == normalizedSearch,
            ExactTitle = search.Length > 0 && EF.Functions.ILike(x.Product.Title, escapedSearch, "\\"),
            TitlePrefix = search.Length > 0 && EF.Functions.ILike(x.Product.Title, $"{escapedSearch}%", "\\"),
            FullTextRank = search.Length > 0
                ? EF.Property<NpgsqlTsVector>(x.Product, "SearchVector").Rank(EF.Functions.WebSearchToTsQuery("english", search))
                : 0f,
            TrigramSimilarity = search.Length > 0 ? EF.Functions.TrigramsWordSimilarity(search, x.Product.SearchDocument) : 0d,
            RegularPrice = x.RegularPriceAmount != null && x.RegularPriceAmount > x.CurrentPriceAmount &&
                           x.RegularPriceCurrency == x.CurrentPriceCurrency && x.RegularPriceObservedAt != null &&
                           x.RegularPriceEvidenceReference != null && x.RegularPriceEvidenceReference != ""
                ? x.RegularPriceAmount
                : null,
            HasSupportedSavings = x.RegularPriceAmount != null && x.RegularPriceAmount > x.CurrentPriceAmount &&
                                  x.RegularPriceCurrency == x.CurrentPriceCurrency && x.RegularPriceObservedAt != null &&
                                  x.RegularPriceEvidenceReference != null && x.RegularPriceEvidenceReference != ""
        });

        var totalCount = await candidates.CountAsync(cancellationToken);
        var ordered = ApplySort(candidates, sort);
        var rows = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var listingIds = rows.Select(x => x.ListingId).ToArray();
        var productIds = rows.Select(x => x.ProductId).Distinct().ToArray();
        var listings = listingIds.Length == 0
            ? []
            : await db.RetailerListings
                .AsNoTracking()
                .Include(x => x.Product).ThenInclude(x => x.Brand)
                .Include(x => x.Product).ThenInclude(x => x.Category)
                .Include(x => x.Retailer)
                .Include(x => x.MerchantPolicy)
                .Include(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
                .Where(x => x.IsEnabled && (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) && x.Retailer.IsEnabled &&
                            x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled && listingIds.Contains(x.Id) &&
                            x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                            x.MerchantPolicy.RequiredAttribution != TestOnlyAttribution)
                .ToListAsync(cancellationToken);
        var selected = listings.ToDictionary(x => x.Id);
        var productImages = await PublicImagesAsync(productIds, now, "DEAL_CARD", cancellationToken);
        var items = rows
            .Where(row => selected.ContainsKey(row.ListingId))
            .Select(row => ToCard(selected[row.ListingId], now, productImages.GetValueOrDefault(row.ProductId)))
            .ToList();

        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name)
            .Where(x => x.IsEnabled)
            .Where(x => db.RetailerListings.Any(listing =>
                listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                listing.Retailer.IsEnabled && listing.Product.Brand.IsEnabled && listing.Product.CategoryId == x.Id &&
                listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution))
            .Select(x => new DiscoveryFacetOption(x.Slug, x.Name)).ToListAsync(cancellationToken);
        var retailers = await db.Retailers.AsNoTracking().OrderBy(x => x.Name)
            .Where(x => x.IsEnabled)
            .Where(x => db.RetailerListings.Any(listing =>
                listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled && listing.RetailerId == x.Id &&
                listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution))
            .Select(x => new DiscoveryFacetOption(x.Key, x.Name)).ToListAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        logger.LogInformation(
            "Discovery query completed in {ElapsedMs}ms with {ResultCount} results, {FilterCount} filters, page {Page}, sort {Sort}, zero results {ZeroResults}.",
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            totalCount,
            AppliedFilterCount(request),
            request.Page,
            DiscoveryRules.SortKey(sort),
            totalCount == 0);

        return new DiscoveryResponse(
            items,
            totalCount,
            DiscoveryRules.SortKey(sort),
            request.Page,
            request.PageSize,
            totalPages,
            request.Page < totalPages,
            new DiscoveryFacetsResponse(categories, retailers));
    }

    private static IOrderedQueryable<DiscoveryCandidate> ApplySort(IQueryable<DiscoveryCandidate> query, DiscoverySort sort) => sort switch
    {
        DiscoverySort.Relevance => query
            .OrderByDescending(x => x.ExactModel)
            .ThenByDescending(x => x.ExactMpn)
            .ThenByDescending(x => x.ExactGtin)
            .ThenByDescending(x => x.ExactTitle)
            .ThenByDescending(x => x.TitlePrefix)
            .ThenByDescending(x => x.FullTextRank)
            .ThenByDescending(x => x.TrigramSimilarity)
            .ThenByDescending(x => x.ObservedAt)
            .ThenBy(x => x.ListingId),
        DiscoverySort.SupportedSavings => query
            .OrderByDescending(x => x.HasSupportedSavings)
            .ThenByDescending(x => x.RegularPrice == null ? 0 : (x.RegularPrice - x.CurrentPrice) / x.RegularPrice)
            .ThenByDescending(x => x.ObservedAt)
            .ThenBy(x => x.ListingId),
        DiscoverySort.LowestPrice => query
            .OrderBy(x => x.CurrentPrice)
            .ThenByDescending(x => x.ObservedAt)
            .ThenBy(x => x.ListingId),
        _ => query.OrderByDescending(x => x.ObservedAt).ThenBy(x => x.ListingId)
    };

    private static DiscoverySort? ParseSort(string? value) => value?.ToLowerInvariant() switch
    {
        null or "" => null,
        "relevance" => DiscoverySort.Relevance,
        "recent" => DiscoverySort.RecentlyChecked,
        "savings" => DiscoverySort.SupportedSavings,
        "price-asc" => DiscoverySort.LowestPrice,
        _ => null
    };

    private static int AppliedFilterCount(DiscoveryQueryRequest request) =>
        new object?[] { request.Category, request.Retailer, request.MinPrice, request.MaxPrice, request.HasReference, request.Freshness, request.Match, request.Availability }
            .Count(value => value is not null && value.ToString()!.Length > 0);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class DiscoveryCandidate
    {
        public Guid ListingId { get; init; }
        public Guid ProductId { get; init; }
        public DateTimeOffset? ObservedAt { get; init; }
        public decimal CurrentPrice { get; init; }
        public bool ExactModel { get; init; }
        public bool ExactMpn { get; init; }
        public bool ExactGtin { get; init; }
        public bool ExactTitle { get; init; }
        public bool TitlePrefix { get; init; }
        public float FullTextRank { get; init; }
        public double TrigramSimilarity { get; init; }
        public decimal? RegularPrice { get; init; }
        public bool HasSupportedSavings { get; init; }
    }

    public async Task<ProductDetailResponse?> GetProductAsync(string slug, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var listing = await db.RetailerListings
            .AsNoTracking()
            .Include(x => x.Product).ThenInclude(x => x.Brand)
            .Include(x => x.Product).ThenInclude(x => x.Category)
            .Include(x => x.Retailer)
            .Include(x => x.MerchantPolicy)
            .Include(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
            .Where(x => x.IsEnabled && (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) &&
                        x.Retailer.IsEnabled && x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled && x.Product.Slug == slug &&
                        x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                        x.MerchantPolicy.RequiredAttribution != TestOnlyAttribution)
            .OrderByDescending(x => x.SourceObservedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return listing is null ? null : await ToProductDetailAsync(listing, now, cancellationToken);
    }

    public async Task<ProductDetailResponse?> GetOfferAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var listing = await db.RetailerListings
            .AsNoTracking()
            .Include(x => x.Product).ThenInclude(x => x.Brand)
            .Include(x => x.Product).ThenInclude(x => x.Category)
            .Include(x => x.Retailer)
            .Include(x => x.MerchantPolicy)
            .Include(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
            .SingleOrDefaultAsync(x => x.Id == listingId && x.IsEnabled &&
                (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) &&
                x.Retailer.IsEnabled && x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled &&
                x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                x.MerchantPolicy.RequiredAttribution != TestOnlyAttribution, cancellationToken);
        return listing is null ? null : await ToProductDetailAsync(listing, now, cancellationToken);
    }

    private async Task<ProductDetailResponse> ToProductDetailAsync(RetailerListing listing, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var offer = ToOffer(listing, now);
        var evidence = offer.EvidenceState switch
        {
            "STRONG" => "The current price comes from a permitted source and the exact product match is verified.",
            "PARTIAL" => "The current offer is available, but some source or product evidence is incomplete.",
            _ => "There is not enough verified evidence for a stronger claim."
        };

        var product = listing.Product;
        var productImage = (await PublicImagesAsync([product.Id], now, "PRODUCT_PAGE", cancellationToken)).GetValueOrDefault(product.Id);
        return new ProductDetailResponse(product.Id, product.Slug, product.Title, product.Brand.Name, product.Category.Name,
            product.VariantAttributes, offer, evidence, productImage);
    }

    public async Task<ProductHistoryResponse?> GetProductHistoryAsync(
        string slug,
        ProductHistoryWindow window,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Where(candidate => candidate.Slug == slug)
            .Select(candidate => new { candidate.Id, candidate.Slug })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        var now = clock.GetUtcNow();
        var windowStart = now.AddDays(-(int)window);
        var qualifying =
            from observation in db.PriceObservations.AsNoTracking()
            join listing in db.RetailerListings.AsNoTracking() on observation.RetailerListingId equals listing.Id
            join policy in db.MerchantPolicies.AsNoTracking() on listing.MerchantPolicyId equals policy.Id
            where listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now)
                && listing.Retailer.IsEnabled && listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled && listing.ProductId == product.Id
                && (listing.MatchState == MatchState.AutoMatched || listing.MatchState == MatchState.Confirmed)
                && listing.Condition == ProductCondition.New
                && listing.IsMarketplaceSeller != true
                && policy.AllowPriceStorage == PolicyPermission.Allowed
                && policy.RequiredAttribution != TestOnlyAttribution
                && policy.AllowPriceHistory == PolicyPermission.Allowed
                && observation.IsPermitted
                && observation.Amount > 0
                && observation.Currency == ProductHistoryRules.SupportedCurrency
                && observation.ObservedAt <= now
            select observation;

        var trackingStart = await qualifying
            .Select(observation => (DateTimeOffset?)observation.ObservedAt)
            .MinAsync(cancellationToken);
        var bounded = await qualifying
            .Where(observation => observation.ObservedAt >= windowStart)
            .OrderBy(observation => observation.ObservedAt)
            .Select(observation => new ProductHistoryObservation(observation.Amount, observation.Currency, observation.ObservedAt))
            .ToListAsync(cancellationToken);
        var evidence = ProductHistoryRules.Evaluate(window, now, bounded, trackingStart);

        logger.LogInformation(
            "Product history projected for Product {ProductId}, window {Window}, state {State}, {ObservationCount} observations and {ObservedDayCount} observed days.",
            product.Id,
            ProductHistoryRules.WindowKey(window),
            evidence.State,
            evidence.ObservationCount,
            evidence.ObservedDayCount);

        return new ProductHistoryResponse(
            product.Id,
            product.Slug,
            ProductHistoryRules.WindowKey(window),
            (int)window,
            evidence.State.ToString().ToUpperInvariant(),
            evidence.TrackingStart,
            evidence.ObservationStart,
            evidence.ObservationEnd,
            evidence.LowestObservedPrice,
            evidence.HighestObservedPrice,
            evidence.ObservationCount,
            evidence.ObservedDayCount,
            evidence.LargestGapDays,
            evidence.CoverageSummary,
            evidence.Interpretation,
            evidence.Points.Select(point => new ProductHistoryPointResponse(
                point.ObservedDate,
                point.LowestPrice,
                point.Currency,
                point.ObservationCount)).ToList());
    }

    public async Task<IReadOnlyList<SavedOfferResponse>> GetSavedOffersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var saves = await db.SavedOffers
            .AsNoTracking()
            .Include(x => x.RetailerListing).ThenInclude(x => x.Product).ThenInclude(x => x.Brand)
            .Include(x => x.RetailerListing).ThenInclude(x => x.Product).ThenInclude(x => x.Category)
            .Include(x => x.RetailerListing).ThenInclude(x => x.Retailer)
            .Include(x => x.RetailerListing).ThenInclude(x => x.MerchantPolicy)
            .Include(x => x.RetailerListing).ThenInclude(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
            .Where(x => x.UserId == userId && x.RetailerListing.IsEnabled &&
                (x.RetailerListing.OfferValidFrom == null || x.RetailerListing.OfferValidFrom <= now) &&
                (x.RetailerListing.OfferValidUntil == null || x.RetailerListing.OfferValidUntil > now) &&
                x.RetailerListing.Retailer.IsEnabled && x.RetailerListing.Product.Brand.IsEnabled &&
                x.RetailerListing.Product.Category.IsEnabled &&
                x.RetailerListing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                x.RetailerListing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (saves.Count == 0) return [];

        var productIds = saves.Select(x => x.RetailerListing.ProductId).Distinct().ToArray();
        var productImages = await PublicImagesAsync(productIds, now, "WISHLIST", cancellationToken);
        return saves.Select(saved =>
        {
            var listing = saved.RetailerListing;
            var offer = ToOffer(listing, now);
            var product = listing.Product;

            return new SavedOfferResponse(
                listing.Id,
                product.Id,
                product.Slug,
                product.Title,
                product.Brand.Name,
                product.Category.Name,
                offer.CurrentPrice,
                offer.RegularPrice,
                offer.SavingsAmount,
                offer.SavingsPercent,
                offer.Currency,
                offer.FreshnessState,
                offer.EvidenceState,
                offer.HistoryState,
                offer.Retailer,
                saved.CreatedAt,
                $"/offers/{listing.Id:D}",
                productImages.GetValueOrDefault(product.Id));
        }).ToList();
    }

    private DealCardResponse ToCard(RetailerListing listing, DateTimeOffset now, ProductImageResponse? productImage)
    {
        var offer = ToOffer(listing, now);
        var disclosure = offer.HandoffMode == "DIRECT_PROVIDER"
            ? "Paid link. As an Amazon Associate I earn from qualifying purchases."
            : listing.MerchantPolicy.DisclosureText;
        return new DealCardResponse(listing.Id, listing.ProductId, listing.Product.Slug, listing.Product.Title, listing.Product.Brand.Name,
            listing.Product.Category.Name, listing.Retailer.Name, listing.CurrentPriceAmount, listing.CurrentPriceCurrency ?? "CAD",
            offer.FreshnessState, offer.EvidenceState, offer.AvailabilityState, EvidenceExplanation(offer.EvidenceState), listing.SourceObservedAt,
            PublicMatchState(listing.MatchState), offer.HistoryState, offer.RegularPrice, offer.SavingsAmount, offer.SavingsPercent,
            $"/offers/{listing.Id:D}", offer.HandoffPath, offer.HandoffUrl, offer.HandoffMode, disclosure, productImage);
    }

    private async Task<IReadOnlyDictionary<Guid, ProductImageResponse>> PublicImagesAsync(
        IReadOnlyCollection<Guid> productIds,
        DateTimeOffset now,
        string placement,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, ProductImageResponse>();
        var rows = await db.ProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId) && image.State == ProductImageState.Active &&
                (image.EffectiveAt == null || image.EffectiveAt <= now) && (image.ExpiresAt == null || image.ExpiresAt > now) &&
                image.AllowedPlacements.Contains(placement))
            .OrderByDescending(image => image.CreatedAt)
            .Select(image => new { image.ProductId, image.Id, image.Width, image.Height })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(image => image.ProductId).ToDictionary(group => group.Key, group =>
        {
            var image = group.First();
            return new ProductImageResponse($"{ProductImage.PublicPathPrefix}{image.Id:D}", image.Width, image.Height);
        });
    }

    private static RetailerOfferResponse ToOffer(RetailerListing listing, DateTimeOffset now)
    {
        var freshness = FreshnessCalculator.Calculate(listing.SourceObservedAt, now);
        var evidence = EvidenceCalculator.Calculate(listing.MerchantPolicy, listing.History, listing.CurrentPriceAmount);
        var handoff = Handoff(listing, now);
        var disclosure = handoff.Mode == "DIRECT_PROVIDER"
            ? "Paid link. As an Amazon Associate I earn from qualifying purchases."
            : listing.MerchantPolicy.DisclosureText;
        var currency = listing.CurrentPriceCurrency ?? "CAD";
        var regularPrice = listing.CurrentPriceAmount.HasValue && listing.RegularPriceCurrency == currency &&
                           listing.RegularPriceObservedAt.HasValue &&
                           DiscoveryRules.SupportedSavings(listing.CurrentPriceAmount.Value, listing.RegularPriceAmount, listing.RegularPriceEvidenceReference)
            ? listing.RegularPriceAmount
            : null;
        var savingsAmount = listing.CurrentPriceAmount.HasValue
            ? DiscoveryRules.SavingsAmount(listing.CurrentPriceAmount.Value, regularPrice, listing.RegularPriceEvidenceReference)
            : null;
        var savingsPercent = listing.CurrentPriceAmount.HasValue
            ? DiscoveryRules.SavingsPercent(listing.CurrentPriceAmount.Value, regularPrice, listing.RegularPriceEvidenceReference)
            : null;
        return new RetailerOfferResponse(listing.Id, listing.Retailer.Name, listing.OriginalTitle, listing.CurrentPriceAmount,
            regularPrice, savingsAmount, savingsPercent, currency, freshness.ToString().ToUpperInvariant(), evidence.ToString().ToUpperInvariant(),
            PublicMatchState(listing.MatchState), listing.History.ToString().ToUpperInvariant(), listing.OnlineAvailability.ToString().ToUpperInvariant(),
            listing.Seller, listing.Condition.ToString().ToUpperInvariant(), listing.RegionAvailabilityContext, listing.ShippingContext, listing.SourceObservedAt,
            handoff.Path, handoff.Url, handoff.Mode, disclosure);
    }

    private static string PublicMatchState(MatchState matchState) => matchState switch
    {
        MatchState.AutoMatched or MatchState.Confirmed => "Offer identity verified",
        MatchState.PossibleMatchReview or MatchState.ManualReview => "Offer identity under review",
        _ => "Offer identity unavailable"
    };

    private static PublicHandoff Handoff(RetailerListing listing, DateTimeOffset now)
    {
        if (!listing.MerchantPolicy.CanUseAffiliateLinks || string.IsNullOrWhiteSpace(listing.ApprovedAffiliateDestinationReference)) return PublicHandoff.None;

        var direct = listing.AffiliateLinks
            .Where(link => link.Status == AffiliateLinkStatus.Active && link.IsUsable(now) &&
                           link.AcquisitionMode == AffiliateLinkAcquisitionMode.OwnerProvided &&
                           link.HandoffMode == AffiliateHandoffMode.DirectProvider &&
                           link.Provider == AffiliateProviderType.AmazonCreators &&
                           link.AffiliateProgram.CanAcceptOwnerProvidedLinks())
            .OrderByDescending(link => link.LastValidatedAt)
            .FirstOrDefault(link =>
                AffiliateUrlPolicy.TryValidateHttps(listing.ApprovedAffiliateDestinationReference, link.AffiliateProgram.DestinationDomains, out var listingDestination) &&
                AffiliateUrlPolicy.TryValidateHttps(link.DestinationUrl, link.AffiliateProgram.DestinationDomains, out var persistedDestination) &&
                AffiliateUrlPolicy.DestinationsMatch(listingDestination!, persistedDestination!) &&
                AffiliateUrlPolicy.TryValidateHttps(link.TrackingUrl, link.AffiliateProgram.TrackingDomains, out _));
        if (direct is not null) return new PublicHandoff(null, direct.TrackingUrl, "DIRECT_PROVIDER");

        var hasApprovedLink = listing.AffiliateLinks.Any(link =>
            link.Status == AffiliateLinkStatus.Active &&
            link.IsUsable(now) &&
            link.AcquisitionMode == AffiliateLinkAcquisitionMode.ProviderGenerated &&
            link.HandoffMode == AffiliateHandoffMode.InternalRedirect &&
            link.AffiliateProgram.CanGenerateLinks() &&
            AffiliateUrlPolicy.TryValidateHttps(
                listing.ApprovedAffiliateDestinationReference,
                link.AffiliateProgram.DestinationDomains,
                out var listingDestination) &&
            AffiliateUrlPolicy.TryValidateHttps(
                link.DestinationUrl,
                link.AffiliateProgram.DestinationDomains,
                out var persistedDestination) &&
            AffiliateUrlPolicy.DestinationsMatch(listingDestination!, persistedDestination!) &&
            AffiliateUrlPolicy.TryValidateHttps(
                link.TrackingUrl,
                link.AffiliateProgram.TrackingDomains,
                out _));

        return hasApprovedLink ? new PublicHandoff($"/go/{listing.Id}", null, "INTERNAL_REDIRECT") : PublicHandoff.None;
    }

    private sealed record PublicHandoff(string? Path, string? Url, string Mode)
    {
        public static readonly PublicHandoff None = new(null, null, "NONE");
    }

    private static string EvidenceExplanation(string state) => state switch
    {
        "STRONG" => "Observed history is available for this permitted fixture source.",
        "PARTIAL" => "Some observations exist, but coverage has gaps.",
        _ => "No verified reference is available for a stronger claim."
    };
}
