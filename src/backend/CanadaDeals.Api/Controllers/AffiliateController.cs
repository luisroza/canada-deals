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
    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> RedirectToApprovedListing(Guid listingId, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("AffiliateHandoff:Enabled")) return NotFound();

        var listing = await db.RetailerListings
            .Include(x => x.MerchantPolicy)
            .Include(x => x.AffiliateLinks).ThenInclude(x => x.AffiliateProgram)
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        if (listing is null || !listing.MerchantPolicy.CanUseAffiliateLinks ||
            string.IsNullOrWhiteSpace(listing.ApprovedAffiliateDestinationReference)) return NotFound();
        var now = clock.GetUtcNow();
        var link = listing.AffiliateLinks
            .Where(candidate => candidate.Status == AffiliateLinkStatus.Active && candidate.IsUsable(now) &&
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
