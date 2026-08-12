using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/price-alerts")]
public sealed class PriceAlertsController(
    DealsDbContext db,
    UserManager<ApplicationUser> userManager,
    IBackgroundJobClient jobs,
    TimeProvider clock,
    ILogger<PriceAlertsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PriceAlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var alerts = await db.PriceAlerts
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new PriceAlertResponse(
                x.ProductId,
                x.Product.Slug,
                x.Product.Title,
                x.TargetPrice,
                x.Currency,
                x.Status.ToString().ToUpperInvariant(),
                x.TargetVersion,
                x.ConsentGrantedAt,
                x.ConsentVersion,
                x.LastEvaluatedAt,
                x.LastTriggeredAt))
            .ToListAsync(cancellationToken);
        return Ok(alerts);
    }

    [HttpPut("{productId:guid}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("price-alert-mutations")]
    [ProducesResponseType<PriceAlertMutationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PriceAlertMutationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(Guid productId, UpsertPriceAlertRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConsentToEmail)
        {
            ModelState.AddModelError(nameof(request.ConsentToEmail), "Confirm that you want this target-price email alert.");
            return ValidationProblem(ModelState);
        }

        if (decimal.Round(request.TargetPrice, 2) != request.TargetPrice)
        {
            ModelState.AddModelError(nameof(request.TargetPrice), "Target price supports at most two decimal places.");
            return ValidationProblem(ModelState);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (!user.EmailConfirmed)
            return Conflict(new ProblemDetails { Title = "Email confirmation required", Detail = "Confirm your email before this alert can become active." });
        if (!await db.Set<Product>().AnyAsync(x => x.Id == productId, cancellationToken))
            return NotFound();

        var now = clock.GetUtcNow();
        var alert = await db.PriceAlerts.SingleOrDefaultAsync(
            x => x.UserId == user.Id && x.ProductId == productId,
            cancellationToken);
        var created = alert is null;
        if (alert is null)
        {
            alert = PriceAlert.Create(
                user.Id,
                productId,
                request.TargetPrice,
                PriceAlert.SupportedCurrency,
                now,
                PriceAlert.CurrentConsentVersion,
                now);
            db.PriceAlerts.Add(alert);
        }
        else
        {
            alert.SetTarget(
                request.TargetPrice,
                PriceAlert.SupportedCurrency,
                now,
                PriceAlert.CurrentConsentVersion,
                now);
        }

        if (!await db.SavedProducts.AnyAsync(x => x.UserId == user.Id && x.ProductId == productId, cancellationToken))
            db.SavedProducts.Add(SavedProduct.Create(user.Id, productId, now));

        await db.SaveChangesAsync(cancellationToken);
        jobs.Enqueue<PriceAlertEvaluationJob>(job => job.RunAsync());
        logger.LogInformation(
            "Account {UserId} set price alert {AlertId} for product {ProductId} at target version {TargetVersion}.",
            user.Id,
            alert.Id,
            productId,
            alert.TargetVersion);

        var response = new PriceAlertMutationResponse(
            productId,
            alert.TargetPrice,
            alert.Currency,
            alert.Status.ToString().ToUpperInvariant(),
            alert.TargetVersion,
            $"We'll notify you when a fresh, verified offer is at or below {alert.TargetPrice:0.00} {alert.Currency}.");
        return StatusCode(created ? StatusCodes.Status201Created : StatusCodes.Status200OK, response);
    }

    [HttpDelete("{productId:guid}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("price-alert-mutations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var alert = await db.PriceAlerts.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ProductId == productId,
            cancellationToken);
        if (alert is not null && alert.Status != PriceAlertStatus.Disabled)
        {
            alert.Disable(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Account {UserId} disabled price alert {AlertId} for product {ProductId}.", userId, alert.Id, productId);
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
