using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.PriceTruth;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Services;

public sealed class CatalogQueryService(DealsDbContext db, TimeProvider clock)
{
    public async Task<DiscoveryResponse> GetDiscoveryAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.RetailerListings
            .AsNoTracking()
            .Include(x => x.Product).ThenInclude(x => x.Brand)
            .Include(x => x.Product).ThenInclude(x => x.Category)
            .Include(x => x.Retailer)
            .Include(x => x.MerchantPolicy)
            .Where(x => x.CurrentPriceAmount != null && x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Product.Title, $"%{term}%") ||
                EF.Functions.ILike(x.Product.Brand.Name, $"%{term}%") ||
                (x.Product.ModelNumber != null && EF.Functions.ILike(x.Product.ModelNumber, $"%{term}%")));
        }

        var listings = await query
            .OrderByDescending(x => x.SourceObservedAt)
            .ThenBy(x => x.Product.Title)
            .Take(50)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var items = listings.Select(x => ToCard(x, now, listings)).ToList();
        return new DiscoveryResponse(items, items.Count, "Most recently checked");
    }

    public async Task<ProductDetailResponse?> GetProductAsync(string slug, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (product is null) return null;

        var listings = await db.RetailerListings
            .AsNoTracking()
            .Include(x => x.Retailer)
            .Include(x => x.MerchantPolicy)
            .Where(x => x.ProductId == product.Id)
            .OrderByDescending(x => x.SourceObservedAt)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var offers = listings.Select(x => ToOffer(x, now)).ToList();
        var primary = offers.FirstOrDefault(x => x.IsSafeComparison && x.CurrentPrice is not null) ?? offers[0];
        var safe = offers.Where(x => x.IsSafeComparison && x.ListingId != primary.ListingId).ToList();
        var related = offers.Where(x => !x.IsSafeComparison).ToList();
        var history = primary.HistoryState switch
        {
            "RELIABLE" => "Reliable history coverage supports a stronger comparison context.",
            "PARTIAL" => "Partial history coverage is available; this is not an all-time-low claim.",
            _ => "Price history unavailable. Current price and retailer evidence are still shown."
        };
        var evidence = primary.EvidenceState switch
        {
            "STRONG" => "Current price and permitted history provide strong evidence.",
            "PARTIAL" => "Some evidence is available, but coverage is incomplete.",
            _ => "There is not enough verified evidence for a stronger claim."
        };

        return new ProductDetailResponse(product.Slug, product.Title, product.Brand.Name, product.Category.Name,
            product.VariantAttributes, primary, safe, related, history, evidence);
    }

    private DealCardResponse ToCard(RetailerListing listing, DateTimeOffset now, IReadOnlyList<RetailerListing> feed)
    {
        var offer = ToOffer(listing, now);
        return new DealCardResponse(listing.Id, listing.Product.Slug, listing.Product.Title, listing.Product.Brand.Name,
            listing.Product.Category.Name, listing.Retailer.Name, listing.CurrentPriceAmount, listing.CurrentPriceCurrency ?? "CAD",
            offer.FreshnessState, offer.EvidenceState, EvidenceExplanation(offer.EvidenceState), listing.SourceObservedAt,
            PublicMatchState(listing.MatchState), offer.HistoryState,
            feed.Any(x => x.ProductId == listing.ProductId && x.Id != listing.Id && ComparisonRules.IsSafeComparison(x)),
            $"/products/{listing.Product.Slug}", $"/go/{listing.Id}", listing.MerchantPolicy.DisclosureText);
    }

    private static RetailerOfferResponse ToOffer(RetailerListing listing, DateTimeOffset now)
    {
        var freshness = FreshnessCalculator.Calculate(listing.SourceObservedAt, now);
        var evidence = EvidenceCalculator.Calculate(listing.MerchantPolicy, listing.History, listing.CurrentPriceAmount);
        return new RetailerOfferResponse(listing.Id, listing.Retailer.Name, listing.OriginalTitle, listing.CurrentPriceAmount,
            listing.CurrentPriceCurrency ?? "CAD", freshness.ToString().ToUpperInvariant(), evidence.ToString().ToUpperInvariant(),
            PublicMatchState(listing.MatchState), listing.History.ToString().ToUpperInvariant(), listing.SourceObservedAt,
            $"/go/{listing.Id}", listing.MerchantPolicy.DisclosureText, ComparisonRules.IsSafeComparison(listing));
    }

    private static string PublicMatchState(MatchState matchState) => matchState switch
    {
        MatchState.AutoMatched or MatchState.Confirmed => "Same product confirmed",
        MatchState.PossibleMatchReview or MatchState.ManualReview => "Review before comparing",
        _ => "No safe comparison available"
    };

    private static string EvidenceExplanation(string state) => state switch
    {
        "STRONG" => "Observed history is available for this permitted fixture source.",
        "PARTIAL" => "Some observations exist, but coverage has gaps.",
        _ => "No verified reference is available for a stronger claim."
    };
}
