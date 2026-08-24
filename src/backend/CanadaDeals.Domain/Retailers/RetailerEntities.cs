using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Affiliates;

namespace CanadaDeals.Domain.Retailers;

public sealed class Retailer
{
    private Retailer() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "CA";
    public bool IsEnabled { get; private set; } = true;

    public static Retailer Create(string key, string name, bool enabled = true)
    {
        ValidateKey(key);
        ValidateName(name);
        return new Retailer
        {
            Id = Guid.NewGuid(), Key = key.Trim(), Name = name.Trim(), IsEnabled = enabled
        };
    }

    public void UpdateAdministrativeName(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 80)
            throw new ArgumentException("A retailer key of at most 80 characters is required.", nameof(key));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
            throw new ArgumentException("A retailer name of at most 160 characters is required.", nameof(name));
    }
}

public sealed class StoreBannerProfile
{
    private StoreBannerProfile() { }

    public Guid Id { get; private set; }
    public Guid RetailerId { get; private set; }
    public Retailer Retailer { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Subtitle { get; private set; } = string.Empty;
    public string? AssetPath { get; private set; }
    public StoreBannerAssetSource AssetSource { get; private set; }
    public PolicyPermission BrandAssetPolicy { get; private set; }
    public AffiliateProviderType? AssetProvider { get; private set; }
    public string? AllowedPlacement { get; private set; }
    public int BannerOrder { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? AssetEvidenceReference { get; private set; }
    public DateTimeOffset? EffectiveAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public static StoreBannerProfile CreateOriginal(
        Guid retailerId,
        string title,
        string subtitle,
        string? assetPath,
        int bannerOrder,
        bool enabled = true)
    {
        if (retailerId == Guid.Empty) throw new ArgumentException("Retailer is required.", nameof(retailerId));
        if (string.IsNullOrWhiteSpace(title) || title.Length > 120) throw new ArgumentException("A banner title of at most 120 characters is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(subtitle) || subtitle.Length > 180) throw new ArgumentException("A banner subtitle of at most 180 characters is required.", nameof(subtitle));
        if (!string.IsNullOrWhiteSpace(assetPath) && !StoreBannerAsset.IsReviewedPath(assetPath))
            throw new ArgumentException("Original assets must use a reviewed GreatDeals banner path.", nameof(assetPath));

        return new StoreBannerProfile
        {
            Id = Guid.NewGuid(), RetailerId = retailerId, Title = title.Trim(), Subtitle = subtitle.Trim(),
            AssetPath = string.IsNullOrWhiteSpace(assetPath) ? null : assetPath.Trim(),
            AssetSource = StoreBannerAssetSource.CanadaDealsOriginal,
            BrandAssetPolicy = PolicyPermission.Unknown,
            AllowedPlacement = "store_banner",
            BannerOrder = bannerOrder,
            IsEnabled = enabled
        };
    }

    public static StoreBannerProfile CreateMerchantApproved(
        Guid retailerId,
        AffiliateProviderType provider,
        string title,
        string subtitle,
        string assetPath,
        int bannerOrder,
        string evidenceReference,
        string allowedPlacement,
        DateTimeOffset effectiveAt,
        DateTimeOffset? expiresAt = null,
        bool enabled = true)
    {
        if (retailerId == Guid.Empty) throw new ArgumentException("Retailer is required.", nameof(retailerId));
        if (provider == AffiliateProviderType.Unknown) throw new ArgumentException("Asset provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(title) || title.Length > 120) throw new ArgumentException("A banner title of at most 120 characters is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(subtitle) || subtitle.Length > 180) throw new ArgumentException("A banner subtitle of at most 180 characters is required.", nameof(subtitle));
        if (!StoreBannerAsset.IsReviewedPath(assetPath))
            throw new ArgumentException("Approved assets must use a reviewed GreatDeals banner path.", nameof(assetPath));
        if (string.IsNullOrWhiteSpace(evidenceReference) || evidenceReference.Length > 500)
            throw new ArgumentException("A redacted asset-rights evidence reference is required and limited to 500 characters.", nameof(evidenceReference));
        if (!string.Equals(allowedPlacement?.Trim(), "store_banner", StringComparison.Ordinal))
            throw new ArgumentException("This asset must explicitly allow the store_banner placement.", nameof(allowedPlacement));
        if (expiresAt.HasValue && expiresAt <= effectiveAt) throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new StoreBannerProfile
        {
            Id = Guid.NewGuid(), RetailerId = retailerId, Title = title.Trim(), Subtitle = subtitle.Trim(),
            AssetPath = assetPath.Trim(), AssetSource = StoreBannerAssetSource.MerchantApprovedAffiliateAsset,
            BrandAssetPolicy = PolicyPermission.Allowed, AssetProvider = provider,
            AssetEvidenceReference = evidenceReference.Trim(), AllowedPlacement = "store_banner",
            EffectiveAt = effectiveAt, ExpiresAt = expiresAt, BannerOrder = bannerOrder, IsEnabled = enabled
        };
    }

    public void UpdateOriginal(string title, string subtitle, string? assetPath, int bannerOrder, bool enabled)
    {
        Apply(CreateOriginal(RetailerId, title, subtitle, assetPath, bannerOrder, enabled));
    }

    public void UpdateMerchantApproved(
        AffiliateProviderType provider,
        string title,
        string subtitle,
        string assetPath,
        int bannerOrder,
        string evidenceReference,
        string allowedPlacement,
        DateTimeOffset effectiveAt,
        DateTimeOffset? expiresAt,
        bool enabled)
    {
        Apply(CreateMerchantApproved(RetailerId, provider, title, subtitle, assetPath, bannerOrder, evidenceReference, allowedPlacement, effectiveAt, expiresAt, enabled));
    }

    public bool IsDisplayable(DateTimeOffset now)
    {
        _ = now;
        return IsEnabled;
    }

    public bool CanUseConfiguredAsset(DateTimeOffset now, string placement = "store_banner")
    {
        if (!StoreBannerAsset.IsReviewedPath(AssetPath))
            return false;
        if (AssetSource == StoreBannerAssetSource.CanadaDealsOriginal) return true;

        return AssetSource == StoreBannerAssetSource.MerchantApprovedAffiliateAsset &&
               BrandAssetPolicy == PolicyPermission.Allowed &&
               AssetProvider is not null and not AffiliateProviderType.Unknown &&
               !string.IsNullOrWhiteSpace(AssetEvidenceReference) &&
               string.Equals(AllowedPlacement, placement, StringComparison.Ordinal) &&
               EffectiveAt.HasValue && EffectiveAt <= now &&
               (!ExpiresAt.HasValue || ExpiresAt > now);
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public void Disable() => SetEnabled(false);

    private void Apply(StoreBannerProfile replacement)
    {
        Title = replacement.Title;
        Subtitle = replacement.Subtitle;
        AssetPath = replacement.AssetPath;
        AssetSource = replacement.AssetSource;
        BrandAssetPolicy = replacement.BrandAssetPolicy;
        AssetProvider = replacement.AssetProvider;
        AllowedPlacement = replacement.AllowedPlacement;
        BannerOrder = replacement.BannerOrder;
        IsEnabled = replacement.IsEnabled;
        AssetEvidenceReference = replacement.AssetEvidenceReference;
        EffectiveAt = replacement.EffectiveAt;
        ExpiresAt = replacement.ExpiresAt;
    }
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
    public bool IsEnabled { get; private set; } = true;
    public DateTimeOffset? OfferValidUntil { get; private set; }
    public ICollection<AffiliateLink> AffiliateLinks { get; private set; } = new List<AffiliateLink>();

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
        string? shippingContext = null,
        DateTimeOffset? offerValidUntil = null) => new()
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
        OnlineAvailability = onlineAvailability, ShippingContext = shippingContext, IsEnabled = true,
        OfferValidUntil = offerValidUntil
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

    public void UpdateAdministrativeDetails(
        string originalTitle,
        string productUrl,
        decimal currentPriceAmount,
        DateTimeOffset observedAt,
        string? retailerSku,
        string? approvedAffiliateDestinationReference,
        string? seller,
        bool? isMarketplaceSeller,
        ProductCondition condition,
        int? packQuantity,
        string? bundleContents,
        IReadOnlyDictionary<string, string>? variantAttributes,
        IReadOnlyDictionary<string, string>? externalIdentifiers,
        OnlineAvailabilityState onlineAvailability,
        string? regionAvailabilityContext,
        string? shippingContext,
        bool enabled,
        DateTimeOffset fetchedAt,
        DateTimeOffset? offerValidUntil)
    {
        if (string.IsNullOrWhiteSpace(originalTitle) || originalTitle.Length > 300) throw new ArgumentException("A listing title of at most 300 characters is required.", nameof(originalTitle));
        if (string.IsNullOrWhiteSpace(productUrl) || productUrl.Length > 1000) throw new ArgumentException("A product URL of at most 1000 characters is required.", nameof(productUrl));
        if (!Enum.IsDefined(condition)) throw new ArgumentOutOfRangeException(nameof(condition));
        if (!Enum.IsDefined(onlineAvailability)) throw new ArgumentOutOfRangeException(nameof(onlineAvailability));

        OriginalTitle = originalTitle.Trim();
        ProductUrl = productUrl.Trim();
        RetailerSku = Normalize(retailerSku);
        ApprovedAffiliateDestinationReference = Normalize(approvedAffiliateDestinationReference);
        Seller = Normalize(seller);
        IsMarketplaceSeller = isMarketplaceSeller;
        Condition = condition;
        PackQuantity = packQuantity;
        BundleContents = Normalize(bundleContents);
        VariantAttributesJson = JsonSerializer.Serialize(variantAttributes ?? new Dictionary<string, string>());
        ExternalIdentifiersJson = JsonSerializer.Serialize(externalIdentifiers ?? new Dictionary<string, string>());
        OnlineAvailability = onlineAvailability;
        RegionAvailabilityContext = Normalize(regionAvailabilityContext);
        ShippingContext = Normalize(shippingContext);
        OfferValidUntil = offerValidUntil;
        IsEnabled = enabled;
        Freshness = FreshnessState.Recent;
        RecordCurrentPrice(currentPriceAmount, "CAD", observedAt, fetchedAt);
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public bool IsPublishedAt(DateTimeOffset now) => IsEnabled && (!OfferValidUntil.HasValue || OfferValidUntil > now);

    public void SetAdministrativeMatchState(MatchState matchState)
    {
        if (!Enum.IsDefined(matchState)) throw new ArgumentOutOfRangeException(nameof(matchState));
        MatchState = matchState;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
