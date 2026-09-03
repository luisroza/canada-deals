using System.Text.Json;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Integrations;

public sealed class CatalogMerchantSource
{
    private CatalogMerchantSource() { }

    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderAdvertiserId { get; private set; } = string.Empty;
    public string CatalogId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public IntegrationPartnershipStatus RelationshipStatus { get; private set; }
    public bool CatalogAvailable { get; private set; }
    public bool AffiliateAvailable { get; private set; }
    public bool? CanadaRelevant { get; private set; }
    public string? Currency { get; private set; }
    public Guid? RetailerId { get; private set; }
    public Retailer? Retailer { get; private set; }
    public Guid? MerchantPolicyId { get; private set; }
    public MerchantPolicy? MerchantPolicy { get; private set; }
    public Guid? DefaultCategoryId { get; private set; }
    public Category? DefaultCategory { get; private set; }
    public string AllowedDestinationHostsJson { get; private set; } = "[]";
    public bool CatalogEnabled { get; private set; }
    public CatalogSourceState State { get; private set; }
    public DateTimeOffset DiscoveredAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CatalogMerchantSource CreateDiscovery(
        string provider,
        string providerAdvertiserId,
        string? catalogId,
        string displayName,
        IntegrationPartnershipStatus relationshipStatus,
        bool catalogAvailable,
        bool affiliateAvailable,
        bool? canadaRelevant,
        string? currency,
        DateTimeOffset now)
    {
        ValidateIdentity(provider, providerAdvertiserId, catalogId, displayName);
        var normalizedCurrency = Normalize(currency)?.ToUpperInvariant();
        if (normalizedCurrency is { Length: not 3 }) throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));
        return new CatalogMerchantSource
        {
            Id = Guid.NewGuid(),
            Provider = NormalizeProvider(provider),
            ProviderAdvertiserId = providerAdvertiserId.Trim(),
            CatalogId = Normalize(catalogId) ?? string.Empty,
            DisplayName = displayName.Trim(),
            RelationshipStatus = relationshipStatus,
            CatalogAvailable = catalogAvailable,
            AffiliateAvailable = affiliateAvailable,
            CanadaRelevant = canadaRelevant,
            Currency = normalizedCurrency,
            CatalogEnabled = false,
            State = CatalogSourceState.Unmapped,
            DiscoveredAt = now,
            UpdatedAt = now
        };
    }

    public void ReconcileDiscovery(
        string displayName,
        IntegrationPartnershipStatus relationshipStatus,
        bool catalogAvailable,
        bool affiliateAvailable,
        bool? canadaRelevant,
        string? currency,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 240)
            throw new ArgumentException("Display name is required and cannot exceed 240 characters.", nameof(displayName));
        var normalizedCurrency = Normalize(currency)?.ToUpperInvariant();
        if (normalizedCurrency is { Length: not 3 }) throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));
        DisplayName = displayName.Trim();
        RelationshipStatus = relationshipStatus;
        CatalogAvailable = catalogAvailable;
        AffiliateAvailable = affiliateAvailable;
        CanadaRelevant = canadaRelevant;
        Currency = normalizedCurrency;
        UpdatedAt = now;
        if (!ProviderAllowsDryRun()) CatalogEnabled = false;
        RecalculateState();
    }

    public void ConfigureMapping(
        Guid? retailerId,
        Guid? merchantPolicyId,
        Guid? defaultCategoryId,
        IEnumerable<string>? allowedDestinationHosts,
        bool catalogEnabled,
        DateTimeOffset now)
    {
        if (catalogEnabled && (!retailerId.HasValue || !merchantPolicyId.HasValue || !defaultCategoryId.HasValue))
            throw new InvalidOperationException("Catalog activation requires explicit Retailer, MerchantPolicy, and default Category mappings.");
        if (catalogEnabled && !ProviderAllowsDryRun())
            throw new InvalidOperationException("Catalog activation requires an active relationship, available catalog, and verified Canada relevance.");

        RetailerId = retailerId;
        MerchantPolicyId = merchantPolicyId;
        DefaultCategoryId = defaultCategoryId;
        var hosts = (allowedDestinationHosts ?? [])
            .Select(host => host.Trim().Trim('.').ToLowerInvariant())
            .Where(host => host.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(host => host, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (hosts.Length > 32 || hosts.Any(host => host.Length > 253 || host.Contains('/') || host.Contains('*') || Uri.CheckHostName(host) == UriHostNameType.Unknown))
            throw new ArgumentException("Destination hosts must contain at most 32 valid host names.", nameof(allowedDestinationHosts));
        AllowedDestinationHostsJson = JsonSerializer.Serialize(hosts);
        if (catalogEnabled && AllowedDestinationHosts.Count == 0)
            throw new InvalidOperationException("Catalog activation requires at least one approved destination host.");
        CatalogEnabled = catalogEnabled;
        UpdatedAt = now;
        RecalculateState();
    }

    public bool ProviderAllowsDryRun() =>
        RelationshipStatus == IntegrationPartnershipStatus.Active &&
        CatalogAvailable && CanadaRelevant == true;

    public bool CanPersist(MerchantPolicy policy) =>
        CatalogEnabled && ProviderAllowsDryRun() &&
        RetailerId.HasValue && DefaultCategoryId.HasValue && MerchantPolicyId == policy.Id &&
        policy.AllowMetadataCaching == PolicyPermission.Allowed &&
        policy.AllowPriceStorage == PolicyPermission.Allowed;

    public IReadOnlyList<string> AllowedDestinationHosts =>
        JsonSerializer.Deserialize<List<string>>(AllowedDestinationHostsJson) ?? [];

    private void RecalculateState()
    {
        if (!RetailerId.HasValue || !MerchantPolicyId.HasValue || !DefaultCategoryId.HasValue)
            State = CatalogSourceState.Unmapped;
        else if (!ProviderAllowsDryRun())
            State = CatalogSourceState.MappedPolicyBlocked;
        else if (!CatalogEnabled)
            State = CatalogSourceState.ReadyForDryRun;
        else
            State = CatalogSourceState.Active;
    }

    private static void ValidateIdentity(string provider, string advertiserId, string? catalogId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Trim().Length > 24)
            throw new ArgumentException("Provider is required and cannot exceed 24 characters.", nameof(provider));
        if (string.IsNullOrWhiteSpace(advertiserId) || advertiserId.Trim().Length > 160)
            throw new ArgumentException("Provider advertiser ID is required and cannot exceed 160 characters.", nameof(advertiserId));
        if (Normalize(catalogId) is { Length: > 160 })
            throw new ArgumentException("Catalog ID cannot exceed 160 characters.", nameof(catalogId));
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 240)
            throw new ArgumentException("Display name is required and cannot exceed 240 characters.", nameof(displayName));
    }

    private static string NormalizeProvider(string value) => value.Trim().ToLowerInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CatalogSourceMapping
{
    private CatalogSourceMapping() { }

    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderAdvertiserId { get; private set; } = string.Empty;
    public string SourceListingKey { get; private set; } = string.Empty;
    public Guid RetailerListingId { get; private set; }
    public RetailerListing RetailerListing { get; private set; } = null!;
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    public static CatalogSourceMapping Create(
        string provider,
        string providerAdvertiserId,
        string sourceListingKey,
        Guid retailerListingId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Trim().Length > 24 ||
            string.IsNullOrWhiteSpace(providerAdvertiserId) || providerAdvertiserId.Trim().Length > 160 ||
            string.IsNullOrWhiteSpace(sourceListingKey) || sourceListingKey.Trim().Length > 240)
            throw new ArgumentException("Provider, advertiser, and source listing identity are required.");
        if (retailerListingId == Guid.Empty) throw new ArgumentException("Retailer listing is required.", nameof(retailerListingId));
        return new CatalogSourceMapping
        {
            Id = Guid.NewGuid(),
            Provider = provider.Trim().ToLowerInvariant(),
            ProviderAdvertiserId = providerAdvertiserId.Trim(),
            SourceListingKey = sourceListingKey.Trim(),
            RetailerListingId = retailerListingId,
            FirstSeenAt = now,
            LastSeenAt = now
        };
    }

    public void MarkSeen(DateTimeOffset now) => LastSeenAt = now;
}

public sealed class CatalogImportRun
{
    private CatalogImportRun() { }

    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderAdvertiserId { get; private set; } = string.Empty;
    public Guid? RetailerId { get; private set; }
    public bool DryRun { get; private set; }
    public IntegrationRunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int PagesFetched { get; private set; }
    public int RecordsReceived { get; private set; }
    public int ValidRecords { get; private set; }
    public int CadRecords { get; private set; }
    public int MappedRecords { get; private set; }
    public int UnmappedRecords { get; private set; }
    public int ListingsCreated { get; private set; }
    public int ListingsUpdated { get; private set; }
    public int ObservationsCreated { get; private set; }
    public int Skipped { get; private set; }
    public int PolicyBlocked { get; private set; }
    public int ReviewCandidates { get; private set; }
    public int UnsupportedCurrency { get; private set; }
    public int Invalid { get; private set; }
    public string? FailureReason { get; private set; }

    public static CatalogImportRun Start(string provider, string providerAdvertiserId, Guid? retailerId, bool dryRun, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Trim().Length > 24 ||
            string.IsNullOrWhiteSpace(providerAdvertiserId) || providerAdvertiserId.Trim().Length > 160)
            throw new ArgumentException("Provider and advertiser are required.");
        return new CatalogImportRun
        {
            Id = Guid.NewGuid(), Provider = provider.Trim().ToLowerInvariant(),
            ProviderAdvertiserId = providerAdvertiserId.Trim(), RetailerId = retailerId,
            DryRun = dryRun, Status = IntegrationRunStatus.Running, StartedAt = now
        };
    }

    public void Complete(
        IntegrationRunStatus status, DateTimeOffset finishedAt, int pagesFetched, int recordsReceived,
        int validRecords, int cadRecords, int mappedRecords, int unmappedRecords,
        int listingsCreated, int listingsUpdated, int observationsCreated, int skipped,
        int policyBlocked, int reviewCandidates, int unsupportedCurrency, int invalid, string? failureReason = null)
    {
        if (status == IntegrationRunStatus.Running) throw new ArgumentException("A terminal status is required.", nameof(status));
        Status = status;
        FinishedAt = finishedAt;
        PagesFetched = NonNegative(pagesFetched);
        RecordsReceived = NonNegative(recordsReceived);
        ValidRecords = NonNegative(validRecords);
        CadRecords = NonNegative(cadRecords);
        MappedRecords = NonNegative(mappedRecords);
        UnmappedRecords = NonNegative(unmappedRecords);
        ListingsCreated = NonNegative(listingsCreated);
        ListingsUpdated = NonNegative(listingsUpdated);
        ObservationsCreated = NonNegative(observationsCreated);
        Skipped = NonNegative(skipped);
        PolicyBlocked = NonNegative(policyBlocked);
        ReviewCandidates = NonNegative(reviewCandidates);
        UnsupportedCurrency = NonNegative(unsupportedCurrency);
        Invalid = NonNegative(invalid);
        if (failureReason?.Trim().Length > 160) throw new ArgumentException("Failure reason cannot exceed 160 characters.", nameof(failureReason));
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
    }

    private static int NonNegative(int value) => Math.Max(0, value);
}
