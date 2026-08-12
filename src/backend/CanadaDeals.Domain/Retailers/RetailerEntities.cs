using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;

namespace CanadaDeals.Domain.Retailers;

public sealed class Retailer
{
    private Retailer() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "CA";

    public static Retailer Create(string key, string name) => new()
    {
        Id = Guid.NewGuid(), Key = key, Name = name
    };
}

public sealed class RetailerListing
{
    private RetailerListing() { }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Catalog.Product Product { get; private set; } = null!;
    public Guid RetailerId { get; private set; }
    public Retailer Retailer { get; private set; } = null!;
    public string ExternalListingId { get; private set; } = string.Empty;
    public string? RetailerSku { get; private set; }
    public string OriginalTitle { get; private set; } = string.Empty;
    public string ProductUrl { get; private set; } = string.Empty;
    public string? ApprovedAffiliateDestinationReference { get; private set; }
    public string? Seller { get; private set; }
    public bool? IsMarketplaceSeller { get; private set; }
    public ProductCondition Condition { get; private set; }
    public string VariantAttributesJson { get; private set; } = "{}";
    public int? PackQuantity { get; private set; }
    public string? BundleContents { get; private set; }
    public string? RegionAvailabilityContext { get; private set; }
    public OnlineAvailabilityState OnlineAvailability { get; private set; }
    public string? ShippingContext { get; private set; }
    public string ExternalIdentifiersJson { get; private set; } = "{}";
    public DateTimeOffset? SourceObservedAt { get; private set; }
    public DateTimeOffset? FetchedAt { get; private set; }
    public FreshnessState Freshness { get; private set; }
    public EvidenceState Evidence { get; private set; }
    public HistoryAvailability History { get; private set; }
    public MatchState MatchState { get; private set; }
    public decimal? CurrentPriceAmount { get; private set; }
    public string? CurrentPriceCurrency { get; private set; }
    public Guid MerchantPolicyId { get; private set; }
    public MerchantPolicy MerchantPolicy { get; private set; } = null!;

    public static RetailerListing Create(
        Guid productId,
        Guid retailerId,
        string externalListingId,
        string originalTitle,
        string productUrl,
        Guid merchantPolicyId,
        MatchState matchState,
        DateTimeOffset? sourceObservedAt,
        DateTimeOffset? fetchedAt,
        decimal? currentPriceAmount,
        string? currentPriceCurrency,
        FreshnessState freshness,
        EvidenceState evidence,
        HistoryAvailability history,
        IReadOnlyDictionary<string, string>? variantAttributes = null,
        IReadOnlyDictionary<string, string>? externalIdentifiers = null,
        string? retailerSku = null,
        string? approvedAffiliateDestinationReference = null,
        string? seller = null,
        bool? isMarketplaceSeller = null,
        ProductCondition condition = ProductCondition.New,
        int? packQuantity = null,
        string? bundleContents = null,
        string? regionAvailabilityContext = "Canada",
        OnlineAvailabilityState onlineAvailability = OnlineAvailabilityState.Available,
        string? shippingContext = null) => new()
    {
        Id = Guid.NewGuid(), ProductId = productId, RetailerId = retailerId,
        ExternalListingId = externalListingId, OriginalTitle = originalTitle, ProductUrl = productUrl,
        MerchantPolicyId = merchantPolicyId, MatchState = matchState, SourceObservedAt = sourceObservedAt,
        FetchedAt = fetchedAt, CurrentPriceAmount = currentPriceAmount, CurrentPriceCurrency = currentPriceCurrency,
        Freshness = freshness, Evidence = evidence, History = history,
        VariantAttributesJson = JsonSerializer.Serialize(variantAttributes ?? new Dictionary<string, string>()),
        ExternalIdentifiersJson = JsonSerializer.Serialize(externalIdentifiers ?? new Dictionary<string, string>()),
        RetailerSku = retailerSku, ApprovedAffiliateDestinationReference = approvedAffiliateDestinationReference,
        Seller = seller, IsMarketplaceSeller = isMarketplaceSeller, Condition = condition,
        PackQuantity = packQuantity, BundleContents = bundleContents, RegionAvailabilityContext = regionAvailabilityContext,
        OnlineAvailability = onlineAvailability, ShippingContext = shippingContext
    };

    public IReadOnlyDictionary<string, string> VariantAttributes => Deserialize(VariantAttributesJson);
    public IReadOnlyDictionary<string, string> ExternalIdentifiers => Deserialize(ExternalIdentifiersJson);

    public void RecordCurrentPrice(decimal amount, string currency, DateTimeOffset observedAt, DateTimeOffset fetchedAt)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (decimal.Round(amount, 2) != amount) throw new ArgumentException("Price supports at most two decimal places.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        CurrentPriceAmount = amount;
        CurrentPriceCurrency = currency.ToUpperInvariant();
        SourceObservedAt = observedAt;
        FetchedAt = fetchedAt;
    }

    private static IReadOnlyDictionary<string, string> Deserialize(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
}

public sealed class PriceObservation
{
    private PriceObservation() { }
    public Guid Id { get; private set; }
    public Guid RetailerListingId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "CAD";
    public DateTimeOffset ObservedAt { get; private set; }
    public DateTimeOffset FetchedAt { get; private set; }
    public bool IsPermitted { get; private set; }
    public string SourceHash { get; private set; } = string.Empty;

    public static PriceObservation Create(Guid listingId, decimal amount, string currency, DateTimeOffset observedAt, DateTimeOffset fetchedAt, bool isPermitted, string sourceHash) => new()
    {
        Id = Guid.NewGuid(), RetailerListingId = listingId, Amount = amount, Currency = currency,
        ObservedAt = observedAt, FetchedAt = fetchedAt, IsPermitted = isPermitted, SourceHash = sourceHash
    };
}
