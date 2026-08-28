using System.Text.Json;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Affiliates;

public sealed class AffiliateProgram
{
    private AffiliateProgram() { }

    public Guid Id { get; private set; }
    public Guid RetailerId { get; private set; }
    public Retailer Retailer { get; private set; } = null!;
    public AffiliateProviderType Provider { get; private set; }
    public AffiliateProgramStatus Status { get; private set; }
    public string? ProviderProgramId { get; private set; }
    public string? MediaPropertyId { get; private set; }
    public string? ProviderLinkReference { get; private set; }
    public bool? AllowsDeepLinking { get; private set; }
    public string DestinationDomainsJson { get; private set; } = "[]";
    public string TrackingDomainsJson { get; private set; } = "[]";
    public string? RelationshipEvidenceReference { get; private set; }
    public DateTimeOffset? RelationshipValidatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<string> DestinationDomains => DeserializeDomains(DestinationDomainsJson);
    public IReadOnlyList<string> TrackingDomains => DeserializeDomains(TrackingDomainsJson);

    public static AffiliateProgram Create(
        Guid retailerId,
        AffiliateProviderType provider,
        AffiliateProgramStatus status,
        DateTimeOffset now,
        string? providerProgramId = null,
        string? mediaPropertyId = null,
        string? providerLinkReference = null,
        bool? allowsDeepLinking = null,
        IEnumerable<string>? destinationDomains = null,
        IEnumerable<string>? trackingDomains = null,
        string? relationshipEvidenceReference = null,
        DateTimeOffset? relationshipValidatedAt = null)
    {
        if (retailerId == Guid.Empty) throw new ArgumentException("Retailer is required.", nameof(retailerId));
        if (provider == AffiliateProviderType.Unknown) throw new ArgumentException("Affiliate provider is required.", nameof(provider));

        var program = new AffiliateProgram
        {
            Id = Guid.NewGuid(),
            RetailerId = retailerId,
            Provider = provider,
            Status = status,
            ProviderProgramId = Normalize(providerProgramId),
            MediaPropertyId = Normalize(mediaPropertyId),
            ProviderLinkReference = Normalize(providerLinkReference),
            AllowsDeepLinking = allowsDeepLinking,
            DestinationDomainsJson = SerializeDomains(destinationDomains),
            TrackingDomainsJson = SerializeDomains(trackingDomains),
            RelationshipEvidenceReference = Normalize(relationshipEvidenceReference),
            RelationshipValidatedAt = relationshipValidatedAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (status == AffiliateProgramStatus.Active) program.EnsureActivationIsComplete();
        return program;
    }

    public void Activate(
        string providerProgramId,
        string mediaPropertyId,
        bool allowsDeepLinking,
        IEnumerable<string> destinationDomains,
        IEnumerable<string> trackingDomains,
        string relationshipEvidenceReference,
        DateTimeOffset validatedAt,
        string? providerLinkReference = null)
    {
        ProviderProgramId = Normalize(providerProgramId);
        MediaPropertyId = Normalize(mediaPropertyId);
        ProviderLinkReference = Normalize(providerLinkReference);
        AllowsDeepLinking = allowsDeepLinking;
        DestinationDomainsJson = SerializeDomains(destinationDomains);
        TrackingDomainsJson = SerializeDomains(trackingDomains);
        RelationshipEvidenceReference = Normalize(relationshipEvidenceReference);
        RelationshipValidatedAt = validatedAt;
        UpdatedAt = validatedAt;
        EnsureActivationIsComplete();
        Status = AffiliateProgramStatus.Active;
    }

    public void SetStatus(AffiliateProgramStatus status, DateTimeOffset now)
    {
        if (status == AffiliateProgramStatus.Active) EnsureActivationIsComplete();
        Status = status;
        UpdatedAt = now;
    }

    public bool CanGenerateLinks() =>
        Provider != AffiliateProviderType.AmazonCreators &&
        Status == AffiliateProgramStatus.Active &&
        RelationshipValidatedAt.HasValue &&
        AllowsDeepLinking == true &&
        DestinationDomains.Count > 0 &&
        TrackingDomains.Count > 0;

    public bool CanAcceptOwnerProvidedLinks() =>
        Provider == AffiliateProviderType.AmazonCreators &&
        Status == AffiliateProgramStatus.Active &&
        !string.IsNullOrWhiteSpace(ProviderProgramId) &&
        !string.IsNullOrWhiteSpace(RelationshipEvidenceReference) &&
        RelationshipValidatedAt.HasValue &&
        DestinationDomains.Count > 0 &&
        TrackingDomains.Count > 0;

    public void ActivateOwnerProvidedAmazon(
        string partnerTag,
        IEnumerable<string> destinationDomains,
        IEnumerable<string> trackingDomains,
        string relationshipEvidenceReference,
        DateTimeOffset validatedAt)
    {
        if (Provider != AffiliateProviderType.AmazonCreators)
            throw new InvalidOperationException("Only an Amazon program can accept an owner-provided Amazon link.");

        ProviderProgramId = Normalize(partnerTag);
        MediaPropertyId = null;
        ProviderLinkReference = "OWNER_PROVIDED";
        AllowsDeepLinking = true;
        DestinationDomainsJson = SerializeDomains(destinationDomains);
        TrackingDomainsJson = SerializeDomains(trackingDomains);
        RelationshipEvidenceReference = Normalize(relationshipEvidenceReference);
        RelationshipValidatedAt = validatedAt;
        UpdatedAt = validatedAt;
        EnsureActivationIsComplete();
        Status = AffiliateProgramStatus.Active;
    }

    private void EnsureActivationIsComplete()
    {
        if (string.IsNullOrWhiteSpace(ProviderProgramId) ||
            (Provider is not AffiliateProviderType.Rakuten and not AffiliateProviderType.AmazonCreators && string.IsNullOrWhiteSpace(MediaPropertyId)) ||
            string.IsNullOrWhiteSpace(RelationshipEvidenceReference) ||
            !RelationshipValidatedAt.HasValue ||
            AllowsDeepLinking != true ||
            DestinationDomains.Count == 0 ||
            TrackingDomains.Count == 0)
        {
            throw new InvalidOperationException("An active affiliate program requires provider IDs, approved relationship evidence, deep-link permission, and destination/tracking domains.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SerializeDomains(IEnumerable<string>? domains) => JsonSerializer.Serialize(
        (domains ?? []).Select(x => x.Trim().TrimEnd('.').ToLowerInvariant())
            .Where(x => x.Length > 0 && !x.Contains('/') && !x.Contains('*'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    private static IReadOnlyList<string> DeserializeDomains(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? [];
}

public sealed class AffiliateLink
{
    private AffiliateLink() { }

    public Guid Id { get; private set; }
    public Guid RetailerListingId { get; private set; }
    public RetailerListing RetailerListing { get; private set; } = null!;
    public Guid AffiliateProgramId { get; private set; }
    public AffiliateProgram AffiliateProgram { get; private set; } = null!;
    public AffiliateProviderType Provider { get; private set; }
    public AffiliateLinkAcquisitionMode AcquisitionMode { get; private set; }
    public AffiliateHandoffMode HandoffMode { get; private set; }
    public string TrackingUrl { get; private set; } = string.Empty;
    public string DestinationUrl { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastValidatedAt { get; private set; }
    public DateTimeOffset RevalidateAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public AffiliateLinkStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public static AffiliateLink CreateActive(
        Guid listingId,
        Guid programId,
        AffiliateProviderType provider,
        string trackingUrl,
        string destinationUrl,
        DateTimeOffset now,
        DateTimeOffset revalidateAt,
        DateTimeOffset? expiresAt = null,
        string? providerReference = null)
    {
        if (listingId == Guid.Empty || programId == Guid.Empty) throw new ArgumentException("Listing and program are required.");
        if (provider == AffiliateProviderType.Unknown) throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(trackingUrl) || string.IsNullOrWhiteSpace(destinationUrl)) throw new ArgumentException("Tracking and destination URLs are required.");
        if (revalidateAt <= now) throw new ArgumentOutOfRangeException(nameof(revalidateAt));
        if (expiresAt.HasValue && expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new AffiliateLink
        {
            Id = Guid.NewGuid(), RetailerListingId = listingId, AffiliateProgramId = programId, Provider = provider,
            AcquisitionMode = AffiliateLinkAcquisitionMode.ProviderGenerated, HandoffMode = AffiliateHandoffMode.InternalRedirect,
            TrackingUrl = trackingUrl, DestinationUrl = destinationUrl, ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim(),
            CreatedAt = now, LastValidatedAt = now, RevalidateAt = revalidateAt, ExpiresAt = expiresAt,
            Status = AffiliateLinkStatus.Active
        };
    }

    public static AffiliateLink CreateOwnerProvidedActive(
        Guid listingId,
        Guid programId,
        AffiliateProviderType provider,
        string trackingUrl,
        string destinationUrl,
        DateTimeOffset now,
        DateTimeOffset revalidateAt,
        DateTimeOffset? expiresAt = null,
        string? providerReference = null)
    {
        if (provider != AffiliateProviderType.AmazonCreators)
            throw new ArgumentException("Direct owner-provided handoff is currently supported only for Amazon.", nameof(provider));
        if (trackingUrl != trackingUrl.Trim() || destinationUrl != destinationUrl.Trim())
            throw new ArgumentException("Owner-provided URLs must not contain leading or trailing whitespace.");
        if (!Uri.TryCreate(trackingUrl, UriKind.Absolute, out var tracking) || tracking.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(tracking.UserInfo) || tracking.Port != 443)
            throw new ArgumentException("The owner-provided tracking URL must be an absolute HTTPS URL on port 443.", nameof(trackingUrl));
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var destination) || destination.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(destination.UserInfo) || destination.Port != 443)
            throw new ArgumentException("The approved destination must be an absolute HTTPS URL on port 443.", nameof(destinationUrl));
        if (listingId == Guid.Empty || programId == Guid.Empty) throw new ArgumentException("Listing and program are required.");
        if (revalidateAt <= now) throw new ArgumentOutOfRangeException(nameof(revalidateAt));
        if (expiresAt.HasValue && expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new AffiliateLink
        {
            Id = Guid.NewGuid(), RetailerListingId = listingId, AffiliateProgramId = programId, Provider = provider,
            AcquisitionMode = AffiliateLinkAcquisitionMode.OwnerProvided, HandoffMode = AffiliateHandoffMode.DirectProvider,
            TrackingUrl = trackingUrl, DestinationUrl = destinationUrl,
            ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? "OWNER_PROVIDED" : providerReference.Trim(),
            CreatedAt = now, LastValidatedAt = now, RevalidateAt = revalidateAt, ExpiresAt = expiresAt,
            Status = AffiliateLinkStatus.Active
        };
    }

    public static AffiliateLink CreateFailure(
        Guid listingId,
        Guid programId,
        AffiliateProviderType provider,
        string destinationUrl,
        string failureReason,
        DateTimeOffset now,
        DateTimeOffset revalidateAt)
    {
        if (listingId == Guid.Empty || programId == Guid.Empty) throw new ArgumentException("Listing and program are required.");
        if (provider == AffiliateProviderType.Unknown) throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(destinationUrl) || string.IsNullOrWhiteSpace(failureReason)) throw new ArgumentException("Destination and failure reason are required.");
        return new AffiliateLink
        {
            Id = Guid.NewGuid(), RetailerListingId = listingId, AffiliateProgramId = programId, Provider = provider,
            AcquisitionMode = AffiliateLinkAcquisitionMode.ProviderGenerated, HandoffMode = AffiliateHandoffMode.InternalRedirect,
            DestinationUrl = destinationUrl, TrackingUrl = string.Empty, CreatedAt = now, LastValidatedAt = now,
            RevalidateAt = revalidateAt, Status = AffiliateLinkStatus.Invalid, FailureReason = failureReason.Trim()
        };
    }

    public bool IsUsable(DateTimeOffset now) => Status == AffiliateLinkStatus.Active && (!ExpiresAt.HasValue || ExpiresAt > now);

    public void Disable(string reason)
    {
        Status = AffiliateLinkStatus.Disabled;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "DISABLED" : reason.Trim();
    }
}

public sealed class StoreAffiliateDestination
{
    private StoreAffiliateDestination() { }

    public Guid Id { get; private set; }
    public Guid RetailerId { get; private set; }
    public Guid AffiliateProgramId { get; private set; }
    public AffiliateProgram AffiliateProgram { get; private set; } = null!;
    public AffiliateProviderType Provider { get; private set; }
    public string TrackingUrl { get; private set; } = string.Empty;
    public string DestinationUrl { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastValidatedAt { get; private set; }
    public DateTimeOffset RevalidateAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public AffiliateLinkStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public static StoreAffiliateDestination CreateActive(
        Guid retailerId,
        Guid programId,
        AffiliateProviderType provider,
        string trackingUrl,
        string destinationUrl,
        DateTimeOffset now,
        DateTimeOffset revalidateAt,
        DateTimeOffset? expiresAt = null,
        string? providerReference = null)
    {
        if (retailerId == Guid.Empty || programId == Guid.Empty) throw new ArgumentException("Retailer and program are required.");
        if (provider == AffiliateProviderType.Unknown) throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(trackingUrl) || string.IsNullOrWhiteSpace(destinationUrl)) throw new ArgumentException("Tracking and destination URLs are required.");
        if (revalidateAt <= now) throw new ArgumentOutOfRangeException(nameof(revalidateAt));
        if (expiresAt.HasValue && expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new StoreAffiliateDestination
        {
            Id = Guid.NewGuid(), RetailerId = retailerId, AffiliateProgramId = programId, Provider = provider,
            TrackingUrl = trackingUrl.Trim(), DestinationUrl = destinationUrl.Trim(),
            ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim(),
            CreatedAt = now, LastValidatedAt = now, RevalidateAt = revalidateAt, ExpiresAt = expiresAt,
            Status = AffiliateLinkStatus.Active
        };
    }

    public bool IsUsable(DateTimeOffset now) => Status == AffiliateLinkStatus.Active && (!ExpiresAt.HasValue || ExpiresAt > now);

    public void Disable(string reason)
    {
        Status = AffiliateLinkStatus.Disabled;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "DISABLED" : reason.Trim();
    }
}

public sealed class ClickEvent
{
    private ClickEvent() { }

    public Guid Id { get; private set; }
    public Guid? AffiliateLinkId { get; private set; }
    public AffiliateLink? AffiliateLink { get; private set; }
    public Guid? RetailerListingId { get; private set; }
    public Guid? StoreAffiliateDestinationId { get; private set; }
    public StoreAffiliateDestination? StoreAffiliateDestination { get; private set; }
    public Guid? RetailerId { get; private set; }
    public Guid? AffiliateProgramId { get; private set; }
    public string Placement { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClickEvent Create(Guid linkId, Guid listingId, string placement, DateTimeOffset now)
    {
        if (linkId == Guid.Empty || listingId == Guid.Empty) throw new ArgumentException("Link and listing are required.");
        if (string.IsNullOrWhiteSpace(placement) || placement.Length > 40) throw new ArgumentException("Placement is required and limited to 40 characters.", nameof(placement));
        return new ClickEvent { Id = Guid.NewGuid(), AffiliateLinkId = linkId, RetailerListingId = listingId, Placement = placement.Trim(), CreatedAt = now };
    }

    public static ClickEvent CreateForStore(Guid destinationId, Guid retailerId, Guid programId, string placement, DateTimeOffset now)
    {
        if (destinationId == Guid.Empty || retailerId == Guid.Empty || programId == Guid.Empty)
            throw new ArgumentException("Destination, retailer, and program are required.");
        if (string.IsNullOrWhiteSpace(placement) || placement.Length > 40)
            throw new ArgumentException("Placement is required and limited to 40 characters.", nameof(placement));
        return new ClickEvent
        {
            Id = Guid.NewGuid(), StoreAffiliateDestinationId = destinationId, RetailerId = retailerId,
            AffiliateProgramId = programId, Placement = placement.Trim(), CreatedAt = now
        };
    }
}
