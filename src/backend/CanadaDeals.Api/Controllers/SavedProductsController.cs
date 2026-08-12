using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/saved-products")]
public sealed class SavedProductsController(
    DealsDbContext db,
    UserManager<ApplicationUser> userManager,
    CatalogQueryService catalog,
    TimeProvider clock,
    ILogger<SavedProductsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SavedProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return Ok(await catalog.GetSavedProductsAsync(userId, cancellationToken));
    }

    [HttpPut("{productId:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<SavedProductMutationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SavedProductMutationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(Guid productId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (!await db.Set<Product>().AnyAsync(x => x.Id == productId, cancellationToken))
            return NotFound();

        if (await db.SavedProducts.AnyAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken))
            return Ok(new SavedProductMutationResponse(productId, true));

        db.SavedProducts.Add(SavedProduct.Create(userId, productId, clock.GetUtcNow()));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.SavedProducts.AnyAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken))
                throw;
            return Ok(new SavedProductMutationResponse(productId, true));
        }

        logger.LogInformation("Account {UserId} saved product {ProductId}.", userId, productId);
        return StatusCode(StatusCodes.Status201Created, new SavedProductMutationResponse(productId, true));
    }

    [HttpDelete("{productId:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsave(Guid productId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (!await db.Set<Product>().AnyAsync(x => x.Id == productId, cancellationToken))
            return NotFound();

        var saved = await db.SavedProducts.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ProductId == productId,
            cancellationToken);
        if (saved is not null)
        {
            db.SavedProducts.Remove(saved);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Account {UserId} unsaved product {ProductId}.", userId, productId);
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
