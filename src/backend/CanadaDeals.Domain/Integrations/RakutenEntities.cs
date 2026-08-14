using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Integrations;

public sealed class RakutenAdvertiserCapability
{
    private RakutenAdvertiserCapability() { }

    public Guid Id { get; private set; }
    public string AdvertiserMid { get; private set; } = string.Empty;
    public string AdvertiserName { get; private set; } = string.Empty;
    public string? AdvertiserUrl { get; private set; }
    public IntegrationAdvertiserStatus AdvertiserStatus { get; private set; }
    public IntegrationPartnershipStatus PartnershipStatus { get; private set; }
    public DateTimeOffset? PartnershipApprovedAt { get; private set; }
    public DateTimeOffset? PartnershipStatusUpdatedAt { get; private set; }
    public string ShipsToJson { get; private set; } = "[]";
    public bool ProductFeedAvailable { get; private set; }
    public bool DeepLinksAvailable { get; private set; }
    public bool? CanadaRelevant { get; private set; }
    public Guid? RetailerId { get; private set; }
    public Retailer? Retailer { get; private set; }
    public Guid? MerchantPolicyId { get; private set; }
    public MerchantPolicy? MerchantPolicy { get; private set; }
    public bool AffiliateEnabled { get; private set; }
    public bool CatalogEnabled { get; private set; }
    public DateTimeOffset CapabilityCheckedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<string> ShipsTo => JsonSerializer.Deserialize<List<string>>(ShipsToJson) ?? [];

    public static RakutenAdvertiserCapability Create(
        string advertiserMid,
        string advertiserName,
        string? advertiserUrl,
        IntegrationAdvertiserStatus advertiserStatus,
        IntegrationPartnershipStatus partnershipStatus,
        IEnumerable<string>? shipsTo,
        bool productFeedAvailable,
        bool deepLinksAvailable,
        DateTimeOffset checkedAt,
        DateTimeOffset? partnershipApprovedAt = null,
        DateTimeOffset? partnershipStatusUpdatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(advertiserMid)) throw new ArgumentException("Rakuten advertiser MID is required.", nameof(advertiserMid));
        if (string.IsNullOrWhiteSpace(advertiserName)) throw new ArgumentException("Rakuten advertiser name is required.", nameof(advertiserName));

        return new RakutenAdvertiserCapability
        {
            Id = Guid.NewGuid(),
            AdvertiserMid = advertiserMid.Trim(),
            AdvertiserName = advertiserName.Trim(),
            AdvertiserUrl = Normalize(advertiserUrl),
            AdvertiserStatus = advertiserStatus,
            PartnershipStatus = partnershipStatus,
            PartnershipApprovedAt = partnershipApprovedAt,
            PartnershipStatusUpdatedAt = partnershipStatusUpdatedAt,
            ShipsToJson = SerializeValues(shipsTo),
            ProductFeedAvailable = productFeedAvailable,
            DeepLinksAvailable = deepLinksAvailable,
            CapabilityCheckedAt = checkedAt,
            UpdatedAt = checkedAt
        };
    }

    public void ReconcileProviderSnapshot(
        string advertiserName,
        string? advertiserUrl,
        IntegrationAdvertiserStatus advertiserStatus,
        IntegrationPartnershipStatus partnershipStatus,
        IEnumerable<string>? shipsTo,
        bool productFeedAvailable,
        bool deepLinksAvailable,
        DateTimeOffset checkedAt,
        DateTimeOffset? partnershipApprovedAt,
        DateTimeOffset? partnershipStatusUpdatedAt)
    {
        AdvertiserName = string.IsNullOrWhiteSpace(advertiserName) ? AdvertiserName : advertiserName.Trim();
        AdvertiserUrl = Normalize(advertiserUrl);
        AdvertiserStatus = advertiserStatus;
        PartnershipStatus = partnershipStatus;
        PartnershipApprovedAt = partnershipApprovedAt;
        PartnershipStatusUpdatedAt = partnershipStatusUpdatedAt;
        ShipsToJson = SerializeValues(shipsTo);
        ProductFeedAvailable = productFeedAvailable;
        DeepLinksAvailable = deepLinksAvailable;
        CapabilityCheckedAt = checkedAt;
        UpdatedAt = checkedAt;

        if (advertiserStatus != IntegrationAdvertiserStatus.Active || partnershipStatus != IntegrationPartnershipStatus.Active)
        {
            AffiliateEnabled = false;
            CatalogEnabled = false;
        }
        if (!deepLinksAvailable) AffiliateEnabled = false;
        if (!productFeedAvailable) CatalogEnabled = false;
    }

    public void MarkProviderUnavailable(DateTimeOffset checkedAt)
    {
        AdvertiserStatus = IntegrationAdvertiserStatus.Unknown;
        PartnershipStatus = IntegrationPartnershipStatus.Unknown;
        ProductFeedAvailable = false;
        DeepLinksAvailable = false;
        AffiliateEnabled = false;
        CatalogEnabled = false;
        CapabilityCheckedAt = checkedAt;
        UpdatedAt = checkedAt;
    }

    public void ConfigureOperatorMapping(
        Guid? retailerId,
        Guid? merchantPolicyId,
        bool? canadaRelevant,
        bool affiliateEnabled,
        bool catalogEnabled,
        DateTimeOffset now)
    {
        if (affiliateEnabled && !CanProviderEnableAffiliate())
            throw new InvalidOperationException("Rakuten affiliate activation requires active advertiser/partnership and deep-link capability.");
        if (catalogEnabled && !CanProviderEnableCatalog())
            throw new InvalidOperationException("Rakuten catalog activation requires active advertiser/partnership and Product Feed capability.");
        if ((affiliateEnabled || catalogEnabled) && (!retailerId.HasValue || !merchantPolicyId.HasValue || canadaRelevant != true))
            throw new InvalidOperationException("Activation requires explicit Canada relevance, Retailer mapping, and MerchantPolicy mapping.");

        RetailerId = retailerId;
        MerchantPolicyId = merchantPolicyId;
        CanadaRelevant = canadaRelevant;
        AffiliateEnabled = affiliateEnabled;
        CatalogEnabled = catalogEnabled;
        UpdatedAt = now;
    }

    public bool CanProviderEnableAffiliate() =>
        AdvertiserStatus == IntegrationAdvertiserStatus.Active &&
        PartnershipStatus == IntegrationPartnershipStatus.Active &&
        DeepLinksAvailable;

    public bool CanProviderEnableCatalog() =>
        AdvertiserStatus == IntegrationAdvertiserStatus.Active &&
        PartnershipStatus == IntegrationPartnershipStatus.Active &&
        ProductFeedAvailable;

    public bool CanGenerateAffiliateLink(MerchantPolicy policy) =>
        CanProviderEnableAffiliate() && AffiliateEnabled && CanadaRelevant == true &&
        RetailerId.HasValue && MerchantPolicyId == policy.Id && policy.CanUseAffiliateLinks;

    public bool CanPersistCatalog(MerchantPolicy policy) =>
        CanProviderEnableCatalog() && CatalogEnabled && CanadaRelevant == true &&
        RetailerId.HasValue && MerchantPolicyId == policy.Id &&
        policy.AllowMetadataCaching == PolicyPermission.Allowed && policy.AllowPriceStorage == PolicyPermission.Allowed;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SerializeValues(IEnumerable<string>? values) => JsonSerializer.Serialize(
        (values ?? []).Select(value => value.Trim().ToUpperInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
}

public sealed class RakutenSourceMapping
{
    private RakutenSourceMapping() { }

    public Guid Id { get; private set; }
    public string AdvertiserMid { get; private set; } = string.Empty;
    public string SourceListingKey { get; private set; } = string.Empty;
    public Guid RetailerListingId { get; private set; }
    public RetailerListing RetailerListing { get; private set; } = null!;
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    public static RakutenSourceMapping Create(string advertiserMid, string sourceListingKey, Guid listingId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(advertiserMid) || string.IsNullOrWhiteSpace(sourceListingKey))
            throw new ArgumentException("Rakuten MID and source listing key are required.");
        if (listingId == Guid.Empty) throw new ArgumentException("Retailer listing is required.", nameof(listingId));
        return new RakutenSourceMapping
        {
            Id = Guid.NewGuid(),
            AdvertiserMid = advertiserMid.Trim(),
            SourceListingKey = sourceListingKey.Trim(),
            RetailerListingId = listingId,
            FirstSeenAt = now,
            LastSeenAt = now
        };
    }

    public void MarkSeen(DateTimeOffset now) => LastSeenAt = now;
}

public sealed class RakutenImportRun
{
    private RakutenImportRun() { }

    public Guid Id { get; private set; }
    public string AdvertiserMid { get; private set; } = string.Empty;
    public bool DryRun { get; private set; }
    public IntegrationRunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int PagesFetched { get; private set; }
    public int RecordsReceived { get; private set; }
    public int ListingsCreated { get; private set; }
    public int ListingsUpdated { get; private set; }
    public int ObservationsCreated { get; private set; }
    public int Skipped { get; private set; }
    public int PolicyBlocked { get; private set; }
    public int ReviewCandidates { get; private set; }
    public string? FailureReason { get; private set; }

    public static RakutenImportRun Start(string advertiserMid, bool dryRun, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(advertiserMid)) throw new ArgumentException("Rakuten advertiser MID is required.", nameof(advertiserMid));
        return new RakutenImportRun
        {
            Id = Guid.NewGuid(),
            AdvertiserMid = advertiserMid.Trim(),
            DryRun = dryRun,
            Status = IntegrationRunStatus.Running,
            StartedAt = now
        };
    }

    public void Complete(
        IntegrationRunStatus status,
        DateTimeOffset finishedAt,
        int pagesFetched,
        int recordsReceived,
        int listingsCreated,
        int listingsUpdated,
        int observationsCreated,
        int skipped,
        int policyBlocked,
        int reviewCandidates,
        string? failureReason = null)
    {
        if (status == IntegrationRunStatus.Running) throw new ArgumentException("A completed run needs a terminal status.", nameof(status));
        Status = status;
        FinishedAt = finishedAt;
        PagesFetched = Math.Max(0, pagesFetched);
        RecordsReceived = Math.Max(0, recordsReceived);
        ListingsCreated = Math.Max(0, listingsCreated);
        ListingsUpdated = Math.Max(0, listingsUpdated);
        ObservationsCreated = Math.Max(0, observationsCreated);
        Skipped = Math.Max(0, skipped);
        PolicyBlocked = Math.Max(0, policyBlocked);
        ReviewCandidates = Math.Max(0, reviewCandidates);
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
    }
}
