using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Affiliates;

public sealed record AffiliateRefreshSummary(int Considered, int Refreshed, int Reused, int Failed);

public sealed class AffiliateLinkRefreshService(
    DealsDbContext db,
    IEnumerable<IAffiliateLinkProvider> providers,
    IOptions<AffiliateOptions> options,
    TimeProvider clock,
    ILogger<AffiliateLinkRefreshService> logger)
{
    private readonly IReadOnlyDictionary<AffiliateProviderType, IAffiliateLinkProvider> _providers =
        providers.ToDictionary(x => x.Provider);
    private readonly AffiliateOptions _options = options.Value;

    public async Task<AffiliateRefreshSummary> RefreshDueAsync(Guid? listingId = null, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var query = db.RetailerListings
            .Include(x => x.Retailer)
            .Include(x => x.MerchantPolicy)
            .Include(x => x.AffiliateLinks)
            .Where(x => x.IsEnabled && (x.OfferValidUntil == null || x.OfferValidUntil > now) &&
                        x.Retailer.IsEnabled && x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled &&
                        x.MerchantPolicy.AllowAffiliateLinks == PolicyPermission.Allowed &&
                        x.ApprovedAffiliateDestinationReference != null &&
                        db.AffiliatePrograms.Any(program => program.RetailerId == x.RetailerId && program.Status == AffiliateProgramStatus.Active));
        if (listingId.HasValue) query = query.Where(x => x.Id == listingId.Value);
        var listings = await query.ToListAsync(cancellationToken);
        var programGroups = (await db.AffiliatePrograms
            .Where(x => x.Status == AffiliateProgramStatus.Active && listings.Select(listing => listing.RetailerId).Contains(x.RetailerId))
            .ToListAsync(cancellationToken))
            .GroupBy(x => x.RetailerId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var refreshed = 0;
        var reused = 0;
        var failed = 0;
        foreach (var listing in listings)
        {
            if (!programGroups.TryGetValue(listing.RetailerId, out var retailerPrograms) || retailerPrograms.Count != 1)
            {
                foreach (var ambiguousProgram in retailerPrograms ?? [])
                    ambiguousProgram.SetStatus(AffiliateProgramStatus.ConfigurationIncomplete, now);
                logger.LogError(
                    "Affiliate link refresh blocked for retailer {RetailerId}, listing {ListingId}: expected exactly one active program, found {ProgramCount}.",
                    listing.RetailerId, listing.Id, retailerPrograms?.Count ?? 0);
                failed++;
                continue;
            }

            var program = retailerPrograms[0];
            var current = listing.AffiliateLinks
                .Where(link => link.AffiliateProgramId == program.Id && link.IsUsable(now))
                .OrderByDescending(link => link.LastValidatedAt)
                .FirstOrDefault();
            if (current is not null && current.RevalidateAt > now)
            {
                reused++;
                continue;
            }

            if (!program.CanGenerateLinks() || !_providers.TryGetValue(program.Provider, out var provider))
            {
                RecordFailure(listing, program, "PROGRAM_OR_PROVIDER_NOT_OPERATIONAL", now, now.AddMinutes(_options.FailureRetryMinutes));
                failed++;
                continue;
            }

            AffiliateLinkResolution resolution;
            try
            {
                resolution = await provider.ResolveAsync(new AffiliateLinkRequest(
                    program, listing.Retailer, listing, "product-page", $"listing-{listing.Id:N}"), cancellationToken);
            }
            catch (HttpRequestException)
            {
                resolution = AffiliateLinkResolution.Failure(program.Provider, AffiliateResolutionStatus.TemporaryFailure, "PROVIDER_NETWORK_FAILURE");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                resolution = AffiliateLinkResolution.Failure(program.Provider, AffiliateResolutionStatus.TemporaryFailure, "PROVIDER_TIMEOUT");
            }

            if (resolution.Status != AffiliateResolutionStatus.Success ||
                !AffiliateUrlPolicy.TryValidateHttps(resolution.TrackingUrl, program.TrackingDomains, out _) ||
                !AffiliateUrlPolicy.TryValidateHttps(resolution.DeepLinkDestination, program.DestinationDomains, out var providerDestination) ||
                !AffiliateUrlPolicy.TryValidateHttps(listing.ApprovedAffiliateDestinationReference, program.DestinationDomains, out var listingDestination) ||
                !AffiliateUrlPolicy.DestinationsMatch(providerDestination!, listingDestination!))
            {
                if (resolution.Status is AffiliateResolutionStatus.RelationshipInactive or AffiliateResolutionStatus.DeepLinkForbidden)
                    program.SetStatus(AffiliateProgramStatus.Suspended, now);
                else if (resolution.Status is AffiliateResolutionStatus.AuthenticationFailed or AffiliateResolutionStatus.ConfigurationIncomplete or AffiliateResolutionStatus.InvalidDestination)
                    program.SetStatus(AffiliateProgramStatus.ConfigurationIncomplete, now);
                RecordFailure(listing, program, resolution.FailureReason ?? resolution.Status.ToString().ToUpperInvariant(), now,
                    resolution.RevalidateAt ?? now.AddMinutes(_options.FailureRetryMinutes));
                logger.LogWarning("Affiliate link refresh failed for provider {Provider}, program {ProgramId}, retailer {RetailerId}, listing {ListingId}: {Status}.",
                    program.Provider, program.Id, listing.RetailerId, listing.Id, resolution.Status);
                failed++;
                continue;
            }

            foreach (var prior in listing.AffiliateLinks.Where(link => link.Status == AffiliateLinkStatus.Active)) prior.Disable("SUPERSEDED");
            db.AffiliateLinks.Add(AffiliateLink.CreateActive(
                listing.Id, program.Id, program.Provider, resolution.TrackingUrl!, resolution.DeepLinkDestination!, now,
                resolution.RevalidateAt ?? now.AddHours(_options.RevalidateHours), resolution.ExpiresAt, resolution.ProviderReference));
            refreshed++;
            logger.LogInformation("Affiliate link refreshed for provider {Provider}, program {ProgramId}, retailer {RetailerId}, listing {ListingId}.",
                program.Provider, program.Id, listing.RetailerId, listing.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AffiliateRefreshSummary(listings.Count, refreshed, reused, failed);
    }

    private void RecordFailure(CanadaDeals.Domain.Retailers.RetailerListing listing, AffiliateProgram program, string reason,
        DateTimeOffset now, DateTimeOffset retryAt)
    {
        db.AffiliateLinks.Add(AffiliateLink.CreateFailure(
            listing.Id, program.Id, program.Provider, listing.ApprovedAffiliateDestinationReference!, reason,
            now, retryAt));
    }
}
