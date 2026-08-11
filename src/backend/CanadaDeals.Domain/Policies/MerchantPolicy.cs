using CanadaDeals.Domain.Common;

namespace CanadaDeals.Domain.Policies;

public sealed class MerchantPolicy
{
    private MerchantPolicy() { }

    public Guid Id { get; private set; }
    public string SourceKey { get; private set; } = string.Empty;
    public PolicyPermission AllowPriceStorage { get; private set; }
    public PolicyPermission AllowPriceHistory { get; private set; }
    public PolicyPermission AllowImageCaching { get; private set; }
    public PolicyPermission AllowMetadataCaching { get; private set; }
    public int? PriceMaxAgeHours { get; private set; }
    public string AllowedComparison { get; private set; } = "UNKNOWN";
    public string RequiredAttribution { get; private set; } = "UNKNOWN";
    public string DisclosureText { get; private set; } = string.Empty;
    public DateTimeOffset? LinkExpiration { get; private set; }
    public int? RawRetentionDays { get; private set; }
    public string DataResidencyNotes { get; private set; } = string.Empty;
    public DateTimeOffset VerifiedAt { get; private set; }

    public static MerchantPolicy Create(
        string sourceKey,
        PolicyPermission allowPriceStorage,
        PolicyPermission allowPriceHistory,
        PolicyPermission allowImageCaching,
        PolicyPermission allowMetadataCaching,
        int? priceMaxAgeHours,
        string allowedComparison,
        string requiredAttribution,
        string disclosureText,
        int? rawRetentionDays,
        string dataResidencyNotes,
        DateTimeOffset verifiedAt)
    {
        if (string.IsNullOrWhiteSpace(sourceKey)) throw new ArgumentException("Source key is required.", nameof(sourceKey));

        return new MerchantPolicy
        {
            Id = Guid.NewGuid(),
            SourceKey = sourceKey.Trim(),
            AllowPriceStorage = allowPriceStorage,
            AllowPriceHistory = allowPriceHistory,
            AllowImageCaching = allowImageCaching,
            AllowMetadataCaching = allowMetadataCaching,
            PriceMaxAgeHours = priceMaxAgeHours,
            AllowedComparison = string.IsNullOrWhiteSpace(allowedComparison) ? "UNKNOWN" : allowedComparison,
            RequiredAttribution = string.IsNullOrWhiteSpace(requiredAttribution) ? "UNKNOWN" : requiredAttribution,
            DisclosureText = disclosureText ?? string.Empty,
            RawRetentionDays = rawRetentionDays,
            DataResidencyNotes = dataResidencyNotes ?? string.Empty,
            VerifiedAt = verifiedAt
        };
    }

    public bool CanPublishCurrentPrice => AllowPriceStorage == PolicyPermission.Allowed;
    public bool CanStoreHistory => AllowPriceHistory == PolicyPermission.Allowed;
    public bool CanCacheImages => AllowImageCaching == PolicyPermission.Allowed;
}
