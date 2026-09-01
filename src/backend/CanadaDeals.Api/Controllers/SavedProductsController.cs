using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/saved-offers")]
[Route("api/v1/saved-products")]
public sealed class SavedOffersController(
    DealsDbContext db,
    UserManager<ApplicationUser> userManager,
    CatalogQueryService catalog,
    TimeProvider clock,
    ILogger<SavedOffersController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SavedOfferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return Ok(await catalog.GetSavedOffersAsync(userId, cancellationToken));
    }

    [HttpPut("{listingId:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<SavedOfferMutationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SavedOfferMutationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var now = clock.GetUtcNow();
        if (!await db.Set<RetailerListing>().AnyAsync(x => x.Id == listingId && x.IsEnabled &&
                (x.OfferValidFrom == null || x.OfferValidFrom <= now) && (x.OfferValidUntil == null || x.OfferValidUntil > now) &&
                x.Retailer.IsEnabled && x.Product.Brand.IsEnabled && x.Product.Category.IsEnabled &&
                x.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed && x.MerchantPolicy.RequiredAttribution != "TEST_ONLY",
                cancellationToken))
            return NotFound();

        if (await db.SavedOffers.AnyAsync(x => x.UserId == userId && x.RetailerListingId == listingId, cancellationToken))
            return Ok(new SavedOfferMutationResponse(listingId, true));

        db.SavedOffers.Add(SavedOffer.Create(userId, listingId, clock.GetUtcNow()));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.SavedOffers.AnyAsync(x => x.UserId == userId && x.RetailerListingId == listingId, cancellationToken))
                throw;
            return Ok(new SavedOfferMutationResponse(listingId, true));
        }

        logger.LogInformation("Account {UserId} saved offer {ListingId}.", userId, listingId);
        return StatusCode(StatusCodes.Status201Created, new SavedOfferMutationResponse(listingId, true));
    }

    [HttpDelete("{listingId:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsave(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (!await db.Set<RetailerListing>().AnyAsync(x => x.Id == listingId, cancellationToken))
            return NotFound();

        var saved = await db.SavedOffers.SingleOrDefaultAsync(
            x => x.UserId == userId && x.RetailerListingId == listingId,
            cancellationToken);
        if (saved is not null)
        {
            db.SavedOffers.Remove(saved);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Account {UserId} unsaved offer {ListingId}.", userId, listingId);
        }

        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var value = userManager.GetUserId(User);
        if (!Guid.TryParse(value, out var userId))
            throw new InvalidOperationException("The authenticated session has no valid user identifier.");
        return userId;
    }
}
