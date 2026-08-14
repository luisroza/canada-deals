using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Infrastructure.Affiliates;

public enum AffiliateResolutionStatus
{
    Success,
    AuthenticationFailed,
    RelationshipInactive,
    DeepLinkForbidden,
    InvalidDestination,
    InvalidTrackingUrl,
    RateLimited,
    TemporaryFailure,
    ConfigurationIncomplete,
    MalformedResponse
}

public sealed record AffiliateLinkRequest(
    AffiliateProgram Program,
    Retailer Retailer,
    RetailerListing Listing,
    string Placement,
    string OpaqueClassification);

public sealed record AffiliateLinkResolution(
    AffiliateResolutionStatus Status,
    AffiliateProviderType Provider,
    string? TrackingUrl = null,
    string? ProviderProgramId = null,
    string? DeepLinkDestination = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? RevalidateAt = null,
    string? ProviderReference = null,
    string? FailureReason = null)
{
    public static AffiliateLinkResolution Failure(AffiliateProviderType provider, AffiliateResolutionStatus status, string reason) =>
        new(status, provider, FailureReason: reason);
}

public interface IAffiliateLinkProvider
{
    AffiliateProviderType Provider { get; }
    Task<AffiliateLinkResolution> ResolveAsync(AffiliateLinkRequest request, CancellationToken cancellationToken = default);
}
