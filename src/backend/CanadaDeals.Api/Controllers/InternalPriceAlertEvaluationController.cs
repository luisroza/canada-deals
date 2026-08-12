using System.Security.Cryptography;
using System.Text;
using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/internal/price-alert-evaluation")]
public sealed class InternalPriceAlertEvaluationController(
    DealsDbContext db,
    UserManager<ApplicationUser> userManager,
    IBackgroundJobClient jobs,
    TimeProvider clock,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("scenarios/{productId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordScenario(
        Guid productId,
        ControlledPriceObservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test")) return NotFound();
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            ModelState.AddModelError(nameof(request.Price), "Price supports at most two decimal places.");
            return ValidationProblem(ModelState);
        }

        var reviewListing = string.Equals(request.ListingScope, "review", StringComparison.OrdinalIgnoreCase);
        if (!reviewListing && !string.Equals(request.ListingScope, "safe", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(request.ListingScope), "Listing scope must be 'safe' or 'review'.");
            return ValidationProblem(ModelState);
        }

        var listing = await db.RetailerListings
            .Include(x => x.MerchantPolicy)
            .Where(x => x.ProductId == productId)
            .Where(x => reviewListing
                ? x.MatchState == MatchState.PossibleMatchReview
                : x.MatchState == MatchState.Confirmed || x.MatchState == MatchState.AutoMatched)
            .OrderBy(x => x.ExternalListingId)
            .FirstOrDefaultAsync(cancellationToken);
        if (listing is null) return NotFound();

        var now = clock.GetUtcNow();
        listing.RecordCurrentPrice(request.Price, PriceAlert.SupportedCurrency, now, now);
        var sourceText = $"controlled:{listing.Id}:{request.Price:0.00}:{now:O}:{Guid.NewGuid()}";
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText)))[..32].ToLowerInvariant();
        var observation = PriceObservation.Create(
            listing.Id,
            request.Price,
            PriceAlert.SupportedCurrency,
            now,
            now,
            listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed,
            sourceHash);
        db.PriceObservations.Add(observation);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ControlledPriceObservationResponse(
            productId,
            listing.Id,
            observation.Id,
            request.Price,
            PriceAlert.SupportedCurrency,
            reviewListing ? "review" : "safe"));
    }

    [HttpPost("run")]
    [ValidateAntiForgeryToken]
    public IActionResult Run()
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test")) return NotFound();
        var jobId = jobs.Enqueue<PriceAlertEvaluationJob>(job => job.RunAsync());
        return Accepted(new AlertEvaluationJobResponse(jobId));
    }

    [HttpGet("jobs/{jobId}")]
    public IActionResult JobStatus(string jobId)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test")) return NotFound();
        var details = JobStorage.Current.GetMonitoringApi().JobDetails(jobId);
        if (details is null) return NotFound();
        var state = details.History.FirstOrDefault()?.StateName ?? "UNKNOWN";
        return Ok(new ControlledJobStatusResponse(jobId, state.ToUpperInvariant()));
    }

    [HttpGet("deliveries")]
    public async Task<IActionResult> Deliveries(Guid? productId, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test")) return NotFound();
        var userId = CurrentUserId();
        var query = db.NotificationDeliveries
            .AsNoTracking()
            .Where(x => x.PriceAlert.UserId == userId);
        if (productId.HasValue) query = query.Where(x => x.PriceAlert.ProductId == productId.Value);

        var deliveries = await query
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ControlledNotificationDeliveryResponse(
                x.Id,
                x.PriceAlertId,
                x.PriceAlert.ProductId,
                x.TargetPrice,
                x.QualifyingPrice,
                x.Currency,
                x.Status.ToString().ToUpperInvariant(),
                x.StatusReason,
                x.CreatedAt,
                x.ProcessedAt))
            .ToListAsync(cancellationToken);
        return Ok(deliveries);
    }

    private Guid CurrentUserId()
    {
        var value = userManager.GetUserId(User);
        if (!Guid.TryParse(value, out var userId))
            throw new InvalidOperationException("The authenticated session has no valid user identifier.");
        return userId;
    }
}
