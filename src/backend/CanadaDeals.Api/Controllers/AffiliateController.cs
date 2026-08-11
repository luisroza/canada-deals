using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("go")]
public sealed class AffiliateController(DealsDbContext db, IConfiguration configuration, ILogger<AffiliateController> logger) : ControllerBase
{
    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> RedirectToApprovedListing(Guid listingId, CancellationToken cancellationToken)
    {
        if (configuration.GetValue<bool?>("AffiliateHandoff:Enabled") == false &&
            !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var listing = await db.RetailerListings.AsNoTracking()
            .Include(x => x.MerchantPolicy)
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        if (listing is null || string.IsNullOrWhiteSpace(listing.ApprovedAffiliateDestinationReference)) return NotFound();
        if (!Uri.TryCreate(listing.ApprovedAffiliateDestinationReference, UriKind.Absolute, out var destination) ||
            (destination.Scheme != Uri.UriSchemeHttps && destination.Scheme != Uri.UriSchemeHttp))
        {
            return Problem("The configured handoff destination is invalid.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var allowedHosts = configuration.GetSection("AffiliateHandoff:AllowedHosts").Get<string[]>() ?? [];
        if (allowedHosts.Length == 0 || !allowedHosts.Contains(destination.Host, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Blocked affiliate handoff for listing {ListingId}: host {Host} is not allowlisted.", listingId, destination.Host);
            return Problem("The handoff destination is not allowlisted.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        logger.LogInformation("Fixture-safe affiliate handoff requested for listing {ListingId}.", listingId);
        return Redirect(destination.ToString());
    }
}
