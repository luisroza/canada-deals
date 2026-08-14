using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public interface IRakutenCapabilityGate
{
    Task<(bool Eligible, string Reason)> CanGenerateAffiliateLinkAsync(string advertiserMid, Guid retailerId, CancellationToken cancellationToken = default);
}

public sealed class RakutenCapabilityGate(DealsDbContext db) : IRakutenCapabilityGate
{
    public async Task<(bool Eligible, string Reason)> CanGenerateAffiliateLinkAsync(
        string advertiserMid,
        Guid retailerId,
        CancellationToken cancellationToken = default)
    {
        var capability = await db.RakutenAdvertiserCapabilities
            .AsNoTracking()
            .Include(candidate => candidate.MerchantPolicy)
            .SingleOrDefaultAsync(candidate => candidate.AdvertiserMid == advertiserMid, cancellationToken);
        if (capability is null) return (false, "RAKUTEN_CAPABILITY_UNKNOWN");
        if (capability.RetailerId != retailerId || capability.MerchantPolicy is null)
            return (false, "RAKUTEN_RETAILER_OR_POLICY_MAPPING_MISSING");
        return capability.CanGenerateAffiliateLink(capability.MerchantPolicy)
            ? (true, "RAKUTEN_AFFILIATE_ELIGIBLE")
            : (false, "RAKUTEN_AFFILIATE_GATE_BLOCKED");
    }
}

public sealed class RakutenAffiliateLinkProvider(
    IRakutenDeepLinkClient deepLinks,
    IRakutenCapabilityGate capabilityGate,
    IOptions<RakutenOptions> options,
    TimeProvider clock) : IAffiliateLinkProvider
{
    private readonly RakutenOptions _options = options.Value;
    public AffiliateProviderType Provider => AffiliateProviderType.Rakuten;

    public async Task<AffiliateLinkResolution> ResolveAsync(AffiliateLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.DeepLinkEnabled)
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "RAKUTEN_DEEP_LINK_DISABLED");
        if (!request.Program.CanGenerateLinks() || string.IsNullOrWhiteSpace(request.Program.ProviderProgramId))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "RAKUTEN_PROGRAM_CONFIGURATION_INCOMPLETE");
        if (!AffiliateUrlPolicy.TryValidateHttps(request.Listing.ApprovedAffiliateDestinationReference, request.Program.DestinationDomains, out var destination))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidDestination, "RAKUTEN_DESTINATION_NOT_APPROVED");

        var gate = await capabilityGate.CanGenerateAffiliateLinkAsync(request.Program.ProviderProgramId, request.Listing.RetailerId, cancellationToken);
        if (!gate.Eligible)
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.RelationshipInactive, gate.Reason);

        try
        {
            var result = await deepLinks.CreateAsync(
                request.Program.ProviderProgramId, destination!.ToString(), OpaqueU1(request.OpaqueClassification), cancellationToken);
            if (!AffiliateUrlPolicy.TryValidateHttps(result.DestinationUrl, request.Program.DestinationDomains, out var returnedDestination) ||
                !AffiliateUrlPolicy.DestinationsMatch(destination, returnedDestination!) ||
                !AffiliateUrlPolicy.TryValidateHttps(result.TrackingUrl, request.Program.TrackingDomains, out _))
                return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidTrackingUrl, "RAKUTEN_RETURNED_URL_NOT_APPROVED");

            var now = clock.GetUtcNow();
            return new AffiliateLinkResolution(
                AffiliateResolutionStatus.Success, Provider, result.TrackingUrl, request.Program.ProviderProgramId,
                result.DestinationUrl, now, null, now.AddHours(168), request.Program.ProviderProgramId);
        }
        catch (RakutenProviderException exception)
        {
            return AffiliateLinkResolution.Failure(Provider, Map(exception.Kind), exception.SafeCode);
        }
    }

    private static string OpaqueU1(string value)
    {
        var safe = new string(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').Take(64).ToArray());
        return safe.Length == 0 ? "listing" : safe;
    }

    private static AffiliateResolutionStatus Map(RakutenFailureKind kind) => kind switch
    {
        RakutenFailureKind.Authentication or RakutenFailureKind.Authorization => AffiliateResolutionStatus.AuthenticationFailed,
        RakutenFailureKind.RateLimited => AffiliateResolutionStatus.RateLimited,
        RakutenFailureKind.PartnershipDenied or RakutenFailureKind.AdvertiserInactive => AffiliateResolutionStatus.RelationshipInactive,
        RakutenFailureKind.DeepLinkDisabled => AffiliateResolutionStatus.DeepLinkForbidden,
        RakutenFailureKind.InvalidDestination => AffiliateResolutionStatus.InvalidDestination,
        RakutenFailureKind.MalformedResponse => AffiliateResolutionStatus.MalformedResponse,
        RakutenFailureKind.ConfigurationError => AffiliateResolutionStatus.ConfigurationIncomplete,
        _ => AffiliateResolutionStatus.TemporaryFailure
    };
}
