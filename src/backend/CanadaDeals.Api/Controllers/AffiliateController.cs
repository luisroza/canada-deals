using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("go")]
public sealed class AffiliateController(DealsDbContext db, IConfiguration configuration, TimeProvider clock, ILogger<AffiliateController> logger) : ControllerBase
{
    [HttpGet("store/{retailerKey}")]
    public async Task<IActionResult> RedirectToApprovedStore(string retailerKey, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("AffiliateHandoff:Enabled")) return NotFound();
        if (string.IsNullOrWhiteSpace(retailerKey) || retailerKey.Length > 80) return NotFound();

        var retailer = await db.Retailers.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Key == retailerKey && candidate.IsEnabled, cancellationToken);
        if (retailer is null) return NotFound();

        var now = clock.GetUtcNow();
        var policyAllowsAffiliate = await db.RetailerListings.AsNoTracking()
            .AnyAsync(listing => listing.IsEnabled && (listing.OfferValidFrom == null || listing.OfferValidFrom <= now) && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                                 listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled && listing.RetailerId == retailer.Id &&
                                 listing.MerchantPolicy.AllowAffiliateLinks == PolicyPermission.Allowed,
                cancellationToken);
        if (!policyAllowsAffiliate) return NotFound();

        var destination = await db.StoreAffiliateDestinations
            .Include(candidate => candidate.AffiliateProgram)
            .Where(candidate => candidate.RetailerId == retailer.Id && candidate.Status == AffiliateLinkStatus.Active)
            .OrderByDescending(candidate => candidate.LastValidatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null || !destination.IsUsable(now)) return NotFound();

        var program = destination.AffiliateProgram;
        if (program.RetailerId != retailer.Id || program.Provider != destination.Provider ||
            program.Status != AffiliateProgramStatus.Active)
            return NotFound();

        if (program.Provider == AffiliateProviderType.Rakuten)
        {
            var capability = await db.RakutenAdvertiserCapabilities
                .AsNoTracking()
                .Include(candidate => candidate.MerchantPolicy)
                .SingleOrDefaultAsync(candidate => candidate.AdvertiserMid == program.ProviderProgramId &&
                                                   candidate.RetailerId == retailer.Id,
                    cancellationToken);
            if (capability?.MerchantPolicy is null || !capability.CanGenerateAffiliateLink(capability.MerchantPolicy))
            {
                logger.LogWarning(
                    "Blocked Rakuten store handoff for program {ProgramId}, retailer {RetailerId}: persisted capability is no longer eligible.",
                    program.Id, retailer.Id);
                return NotFound();
            }
        }

        if (!program.CanGenerateLinks() ||
            !AffiliateUrlPolicy.TryValidateHttps(destination.DestinationUrl, program.DestinationDomains, out _) ||
            !AffiliateUrlPolicy.TryValidateHttps(destination.TrackingUrl, program.TrackingDomains, out var trackingDestination))
        {
            logger.LogWarning(
                "Blocked store affiliate handoff for provider {Provider}, program {ProgramId}, retailer {RetailerId}: URL policy validation failed.",
                program.Provider, program.Id, retailer.Id);
            return Problem("The handoff is not currently available.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        db.ClickEvents.Add(ClickEvent.CreateForStore(destination.Id, retailer.Id, program.Id, "store_banner", now));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Store affiliate handoff accepted for provider {Provider}, program {ProgramId}, retailer {RetailerId}.",
            program.Provider, program.Id, retailer.Id);
        return Redirect(trackingDestination!.ToString());
    }

    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> RedirectToApprovedListing(Guid listingId, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("AffiliateHandoff:Enabled")) return NotFound();

        var now = clock.GetUtcNow();
        var listing = await db.RetailerListings
            .Include(x => x.MerchantPolicy)
            .Include(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
            .SingleOrDefaultAsync(x => x.IsEnabled && (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) &&
                x.Retailer.IsEnabled && x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled && x.Id == listingId, cancellationToken);

        if (listing is null || !listing.MerchantPolicy.CanUseAffiliateLinks ||
            string.IsNullOrWhiteSpace(listing.ApprovedAffiliateDestinationReference)) return NotFound();
        var link = listing.AffiliateLinks
            .Where(candidate => candidate.Status == AffiliateLinkStatus.Active && candidate.IsUsable(now) &&
                                candidate.AcquisitionMode == AffiliateLinkAcquisitionMode.ProviderGenerated &&
                                candidate.HandoffMode == AffiliateHandoffMode.InternalRedirect &&
                                candidate.AffiliateProgram.Status == AffiliateProgramStatus.Active)
            .OrderByDescending(candidate => candidate.LastValidatedAt)
            .FirstOrDefault();
        if (link is null) return NotFound();

        var program = link.AffiliateProgram;
        if (program.Provider == AffiliateProviderType.Rakuten)
        {
            var capability = await db.RakutenAdvertiserCapabilities
                .AsNoTracking()
                .Include(candidate => candidate.MerchantPolicy)
                .SingleOrDefaultAsync(candidate => candidate.AdvertiserMid == program.ProviderProgramId &&
                                                   candidate.RetailerId == listing.RetailerId,
                    cancellationToken);
            if (capability?.MerchantPolicy is null ||
                capability.MerchantPolicyId != listing.MerchantPolicyId ||
                !capability.CanGenerateAffiliateLink(capability.MerchantPolicy))
            {
                logger.LogWarning(
                    "Blocked Rakuten affiliate handoff for program {ProgramId}, retailer {RetailerId}, listing {ListingId}: persisted capability is no longer eligible.",
                    program.Id, listing.RetailerId, listingId);
                return NotFound();
            }
        }

        if (!program.CanGenerateLinks() ||
            !AffiliateUrlPolicy.TryValidateHttps(listing.ApprovedAffiliateDestinationReference, program.DestinationDomains, out var listingDestination) ||
            !AffiliateUrlPolicy.TryValidateHttps(link.DestinationUrl, program.DestinationDomains, out var persistedDestination) ||
            !AffiliateUrlPolicy.DestinationsMatch(listingDestination!, persistedDestination!) ||
            !AffiliateUrlPolicy.TryValidateHttps(link.TrackingUrl, program.TrackingDomains, out var trackingDestination))
        {
            logger.LogWarning("Blocked affiliate handoff for provider {Provider}, program {ProgramId}, listing {ListingId}: URL policy validation failed.",
                program.Provider, program.Id, listingId);
            return Problem("The handoff is not currently available.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        db.ClickEvents.Add(ClickEvent.Create(link.Id, listing.Id, "product-page", now));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Affiliate handoff accepted for provider {Provider}, program {ProgramId}, retailer {RetailerId}, listing {ListingId}.",
            program.Provider, program.Id, listing.RetailerId, listingId);
        return Redirect(trackingDestination!.ToString());
    }
}
