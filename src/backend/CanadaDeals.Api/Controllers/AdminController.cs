using System.Security.Claims;
using System.Text.RegularExpressions;
using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Security;
using CanadaDeals.Domain.Administration;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = AdminAccess.Policy)]
[EnableRateLimiting("admin")]
public sealed class AdminController(DealsDbContext db, TimeProvider clock, ILogger<AdminController> logger) : ControllerBase
{
    private const string TestOnlyAttribution = "TEST_ONLY";
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [HttpGet("session")]
    public ActionResult<AdminSessionResponse> Session() =>
        Ok(new AdminSessionResponse(true, true, User.Identity?.Name));

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var offers = await db.RetailerListings.AsNoTracking()
            .Include(listing => listing.Product).ThenInclude(product => product.Brand)
            .Include(listing => listing.Product).ThenInclude(product => product.Category)
            .Include(listing => listing.Retailer)
            .Include(listing => listing.MerchantPolicy)
            .OrderByDescending(listing => listing.FetchedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var retailers = await db.Retailers.AsNoTracking().OrderBy(retailer => retailer.Name).ToListAsync(cancellationToken);
        var profiles = await db.StoreBannerProfiles.AsNoTracking().ToDictionaryAsync(profile => profile.RetailerId, cancellationToken);
        var reports = await db.ListingIssueReports.AsNoTracking()
            .Include(report => report.RetailerListing).ThenInclude(listing => listing.Retailer)
            .OrderBy(report => report.Status)
            .ThenByDescending(report => report.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var audit = await db.AdminAuditEvents.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(30).ToListAsync(cancellationToken);

        var offerResponses = offers.Select(ToOfferResponse).ToList();
        var bannerResponses = retailers.Select(retailer => ToBannerResponse(retailer, profiles.GetValueOrDefault(retailer.Id), now)).ToList();
        var counts = new AdminDashboardCounts(
            offerResponses.Count(offer => offer.IsEnabled),
            offerResponses.Count(offer => !offer.IsEnabled),
            bannerResponses.Count(banner => banner.IsEnabled && banner.RightsState == "READY"),
            bannerResponses.Count(banner => banner.VisibilityState is "EXPIRED" or "BLOCKED"),
            reports.Count(report => report.Status == ListingIssueStatus.Open));

        return Ok(new AdminDashboardResponse(
            counts,
            await db.Brands.AsNoTracking().OrderBy(brand => brand.Name).Select(brand => new AdminReferenceOption(brand.Id, brand.Slug, brand.Name)).ToListAsync(cancellationToken),
            await db.Categories.AsNoTracking().OrderBy(category => category.Name).Select(category => new AdminReferenceOption(category.Id, category.Slug, category.Name)).ToListAsync(cancellationToken),
            retailers.Select(retailer => new AdminReferenceOption(retailer.Id, retailer.Key, retailer.Name, retailer.IsEnabled)).ToList(),
            await db.MerchantPolicies.AsNoTracking().OrderBy(policy => policy.SourceKey).Select(policy => new AdminPolicyOption(
                policy.Id, policy.SourceKey, policy.AllowPriceStorage.ToString().ToUpper(), policy.AllowPriceHistory.ToString().ToUpper(),
                policy.AllowAffiliateLinks.ToString().ToUpper(), policy.RequiredAttribution)).ToListAsync(cancellationToken),
            offerResponses,
            bannerResponses,
            reports.Select(report => new AdminReportResponse(
                report.Id, report.RetailerListingId, report.RetailerListing.Retailer.Name, report.RetailerListing.OriginalTitle,
                report.Reason.ToContract(), report.Note, report.Status.ToContract(), report.CreatedAt, report.UpdatedAt)).ToList(),
            audit.Select(item => new AdminAuditResponse(item.Id, item.Action, item.EntityType, item.EntityId, item.Summary, item.CreatedAt)).ToList()));
    }

    [HttpPut("reports/{reportId:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReportStatus(Guid reportId, UpdateAdminReportRequest request, CancellationToken cancellationToken)
    {
        var report = await db.ListingIssueReports.SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null) return NotFound();
        if (!ListingIssueReportContractValues.TryParseStatus(request.Status, out var status))
        {
            ModelState.AddModelError(nameof(request.Status), "Choose OPEN, REVIEWED, RESOLVED, or DISMISSED.");
            return ValidationProblem(ModelState);
        }

        var previous = report.Status;
        report.ChangeStatus(status, clock.GetUtcNow());
        db.AdminAuditEvents.Add(Audit("STATUS_CHANGE", "ListingIssueReport", report.Id,
            $"Changed report status from {previous.ToContract()} to {status.ToContract()}. Resolution: {request.ResolutionNote.Trim()}."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} changed Report {ReportId} from {PreviousStatus} to {Status}.", ActorId(), report.Id, previous, status);
        return NoContent();
    }

    [HttpPost("offers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOffer(UpsertAdminOfferRequest request, CancellationToken cancellationToken)
    {
        var context = await ValidateOfferAsync(request, null, cancellationToken);
        if (context is null) return ValidationProblem(ModelState);

        var product = Product.Create(
            request.Slug.Trim(), request.ProductTitle.Trim(), context.Brand, context.Category,
            Normalize(request.ModelNumber), Normalize(request.ManufacturerPartNumber), Normalize(request.Gtin), request.VariantAttributes);
        var listing = RetailerListing.Create(
            product.Id, context.Retailer.Id, request.ExternalListingId.Trim(), request.OriginalTitle.Trim(), request.ProductUrl.Trim(),
            context.Policy.Id, context.MatchState, request.ObservedAt, request.FetchedAt, request.CurrentPrice, "CAD", FreshnessState.Recent,
            context.Policy.CanPublishCurrentPrice ? EvidenceState.Partial : EvidenceState.Unavailable, HistoryAvailability.Unavailable,
            request.VariantAttributes, request.ExternalIdentifiers, Normalize(request.RetailerSku), Normalize(request.ApprovedAffiliateDestinationReference),
            Normalize(request.Seller), request.IsMarketplaceSeller, context.Condition, request.PackQuantity, Normalize(request.BundleContents),
            Normalize(request.RegionAvailabilityContext), context.Availability, Normalize(request.ShippingContext));
        listing.SetEnabled(request.IsEnabled);

        db.AddRange(product, listing);
        db.AdminAuditEvents.Add(Audit("CREATE", "RetailerListing", listing.Id, request.IsEnabled ? "Created and published an administrative offer." : "Created an administrative offer draft."));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin offer creation conflicted with an existing record.");
            return Conflict(new ProblemDetails { Title = "Offer already exists", Detail = "The product slug or retailer listing ID is already in use." });
        }

        logger.LogInformation("Owner admin {UserId} created Listing {ListingId}.", ActorId(), listing.Id);
        return Created($"/api/v1/admin/offers/{listing.Id}", new { listingId = listing.Id, productId = product.Id, previewPath = $"/products/{product.Slug}" });
    }

    [HttpPut("offers/{listingId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOffer(Guid listingId, UpsertAdminOfferRequest request, CancellationToken cancellationToken)
    {
        var listing = await db.RetailerListings
            .Include(item => item.Product).ThenInclude(product => product.Brand)
            .Include(item => item.Product).ThenInclude(product => product.Category)
            .Include(item => item.Retailer)
            .Include(item => item.MerchantPolicy)
            .SingleOrDefaultAsync(item => item.Id == listingId, cancellationToken);
        if (listing is null) return NotFound();

        var context = await ValidateOfferAsync(request, listing, cancellationToken);
        if (context is null) return ValidationProblem(ModelState);
        if ((listing.IsEnabled && !request.IsEnabled || listing.MatchState != context.MatchState) && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating an offer or changing its match decision.");
            return ValidationProblem(ModelState);
        }

        listing.Product.UpdateAdministrativeIdentity(
            request.Slug.Trim(), request.ProductTitle.Trim(), context.Brand, context.Category,
            Normalize(request.ModelNumber), Normalize(request.ManufacturerPartNumber), Normalize(request.Gtin), request.VariantAttributes);
        listing.UpdateAdministrativeDetails(
            request.OriginalTitle, request.ProductUrl, request.CurrentPrice, request.ObservedAt, request.RetailerSku,
            request.ApprovedAffiliateDestinationReference, request.Seller, request.IsMarketplaceSeller, context.Condition,
            request.PackQuantity, request.BundleContents, request.VariantAttributes, request.ExternalIdentifiers, context.Availability,
            request.RegionAvailabilityContext, request.ShippingContext, request.IsEnabled, request.FetchedAt);
        listing.SetAdministrativeMatchState(context.MatchState);
        db.AdminAuditEvents.Add(Audit("UPDATE", "RetailerListing", listing.Id,
            $"Updated administrative offer. Reason: {Normalize(request.ChangeReason) ?? "Routine editorial update"}."));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin offer update conflicted for Listing {ListingId}.", listingId);
            return Conflict(new ProblemDetails { Title = "Offer update conflict", Detail = "The product slug or retailer listing ID conflicts with an existing record." });
        }

        logger.LogInformation("Owner admin {UserId} updated Listing {ListingId}.", ActorId(), listing.Id);
        return NoContent();
    }

    [HttpPut("banners/{retailerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertBanner(Guid retailerId, UpsertAdminBannerRequest request, CancellationToken cancellationToken)
    {
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == retailerId, cancellationToken);
        if (retailer is null) return NotFound();
        var profile = await db.StoreBannerProfiles.SingleOrDefaultAsync(item => item.RetailerId == retailerId, cancellationToken);
        var isNewProfile = profile is null;
        if (profile?.IsEnabled == true && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when disabling a banner.");
            return ValidationProblem(ModelState);
        }

        try
        {
            if (string.Equals(request.AssetSource, "CANADADEALSORIGINAL", StringComparison.OrdinalIgnoreCase))
            {
                if (profile is null)
                {
                    profile = StoreBannerProfile.CreateOriginal(retailerId, request.Title, request.Subtitle, request.AssetPath, request.BannerOrder, request.IsEnabled);
                    db.StoreBannerProfiles.Add(profile);
                }
                else profile.UpdateOriginal(request.Title, request.Subtitle, request.AssetPath, request.BannerOrder, request.IsEnabled);
            }
            else if (string.Equals(request.AssetSource, "MERCHANTAPPROVEDAFFILIATEASSET", StringComparison.OrdinalIgnoreCase) &&
                     Enum.TryParse<AffiliateProviderType>(request.AssetProvider, true, out var provider) && provider != AffiliateProviderType.Unknown &&
                     request.EffectiveAt.HasValue)
            {
                if (profile is null)
                {
                    profile = StoreBannerProfile.CreateMerchantApproved(retailerId, provider, request.Title, request.Subtitle, request.AssetPath ?? string.Empty,
                        request.BannerOrder, request.AssetEvidenceReference ?? string.Empty, request.AllowedPlacement ?? string.Empty, request.EffectiveAt.Value,
                        request.ExpiresAt, request.IsEnabled);
                    db.StoreBannerProfiles.Add(profile);
                }
                else profile.UpdateMerchantApproved(provider, request.Title, request.Subtitle, request.AssetPath ?? string.Empty,
                    request.BannerOrder, request.AssetEvidenceReference ?? string.Empty, request.AllowedPlacement ?? string.Empty, request.EffectiveAt.Value,
                    request.ExpiresAt, request.IsEnabled);
            }
            else
            {
                ModelState.AddModelError(nameof(request.AssetSource), "Choose CANADADEALSORIGINAL or provide every required field for MERCHANTAPPROVEDAFFILIATEASSET.");
                return ValidationProblem(ModelState);
            }
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(request.AssetPath), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.AdminAuditEvents.Add(Audit(isNewProfile ? "CREATE" : "UPDATE", "StoreBannerProfile", profile.Id,
            $"Updated the banner for Retailer {retailer.Id}. Reason: {Normalize(request.ChangeReason) ?? "Routine editorial update"}."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} updated StoreBannerProfile {ProfileId}.", ActorId(), profile.Id);
        return NoContent();
    }

    private async Task<OfferValidationContext?> ValidateOfferAsync(UpsertAdminOfferRequest request, RetailerListing? existing, CancellationToken cancellationToken)
    {
        if (!SlugPattern.IsMatch(request.Slug ?? string.Empty)) ModelState.AddModelError(nameof(request.Slug), "Use lowercase letters, numbers, and single hyphens.");
        ValidateHttpsUrl(request.ProductUrl, nameof(request.ProductUrl), required: true);
        ValidateHttpsUrl(request.ApprovedAffiliateDestinationReference, nameof(request.ApprovedAffiliateDestinationReference), required: false);
        ValidateDictionary(request.VariantAttributes, nameof(request.VariantAttributes));
        ValidateDictionary(request.ExternalIdentifiers, nameof(request.ExternalIdentifiers));
        var now = clock.GetUtcNow();
        if (request.ObservedAt > request.FetchedAt) ModelState.AddModelError(nameof(request.ObservedAt), "Observed time cannot be later than fetched time.");
        if (request.FetchedAt > now.AddMinutes(5)) ModelState.AddModelError(nameof(request.FetchedAt), "Fetched time cannot be in the future.");
        if (decimal.Round(request.CurrentPrice, 2) != request.CurrentPrice) ModelState.AddModelError(nameof(request.CurrentPrice), "Price supports at most two decimal places.");

        if (!Enum.TryParse<ProductCondition>(request.ConditionState, true, out var condition) || !Enum.IsDefined(condition))
            ModelState.AddModelError(nameof(request.ConditionState), "Choose NEW, USED, REFURBISHED, or UNKNOWN.");
        if (!Enum.TryParse<OnlineAvailabilityState>(request.AvailabilityState, true, out var availability) || !Enum.IsDefined(availability))
            ModelState.AddModelError(nameof(request.AvailabilityState), "Choose AVAILABLE, UNAVAILABLE, or UNKNOWN.");
        if (!Enum.TryParse<MatchState>(request.MatchState, true, out var matchState) || !Enum.IsDefined(matchState))
            ModelState.AddModelError(nameof(request.MatchState), "Choose CONFIRMED, MANUALREVIEW, POSSIBLEMATCHREVIEW, or NOMATCH.");

        var brand = await db.Brands.SingleOrDefaultAsync(item => item.Id == request.BrandId, cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken);
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == request.RetailerId, cancellationToken);
        var policy = await db.MerchantPolicies.SingleOrDefaultAsync(item => item.Id == request.MerchantPolicyId, cancellationToken);
        if (brand is null) ModelState.AddModelError(nameof(request.BrandId), "Choose an existing brand.");
        if (category is null) ModelState.AddModelError(nameof(request.CategoryId), "Choose an existing category.");
        if (retailer is null || !retailer.IsEnabled) ModelState.AddModelError(nameof(request.RetailerId), "Choose an enabled retailer.");
        if (policy is null) ModelState.AddModelError(nameof(request.MerchantPolicyId), "Choose an existing merchant policy.");
        if (existing is not null)
        {
            if (existing.RetailerId != request.RetailerId) ModelState.AddModelError(nameof(request.RetailerId), "Retailer cannot be changed after creation.");
            if (existing.MerchantPolicyId != request.MerchantPolicyId) ModelState.AddModelError(nameof(request.MerchantPolicyId), "Merchant policy cannot be changed after creation.");
            if (!string.Equals(existing.ExternalListingId, request.ExternalListingId.Trim(), StringComparison.Ordinal)) ModelState.AddModelError(nameof(request.ExternalListingId), "External listing ID cannot be changed after creation.");
        }
        if (request.IsEnabled && policy is not null && (!policy.CanPublishCurrentPrice || string.Equals(policy.RequiredAttribution, TestOnlyAttribution, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(request.IsEnabled), "This merchant policy does not permit a public current-price offer. Save it as a draft.");
        if (!string.IsNullOrWhiteSpace(request.ApprovedAffiliateDestinationReference) && policy?.CanUseAffiliateLinks != true)
            ModelState.AddModelError(nameof(request.ApprovedAffiliateDestinationReference), "The selected merchant policy does not permit affiliate links.");

        return ModelState.IsValid && brand is not null && category is not null && retailer is not null && policy is not null
            ? new OfferValidationContext(brand, category, retailer, policy, condition, availability, matchState)
            : null;
    }

    private AdminOfferResponse ToOfferResponse(RetailerListing listing)
    {
        var publicEligible = listing.IsEnabled && listing.MerchantPolicy.CanPublishCurrentPrice &&
                             !string.Equals(listing.MerchantPolicy.RequiredAttribution, TestOnlyAttribution, StringComparison.OrdinalIgnoreCase);
        var readiness = publicEligible ? "Ready for public discovery; affiliate handoff remains derived from an approved active link."
            : listing.IsEnabled ? "Blocked by merchant policy." : "Draft or deactivated; not visible in public discovery.";
        return new AdminOfferResponse(
            listing.Id, listing.ProductId, listing.Product.Slug, listing.Product.Title, listing.Product.BrandId, listing.Product.Brand.Name,
            listing.Product.CategoryId, listing.Product.Category.Name, listing.Product.ModelNumber, listing.Product.ManufacturerPartNumber, listing.Product.Gtin,
            listing.Product.VariantAttributes, listing.RetailerId, listing.Retailer.Name, listing.MerchantPolicyId, listing.MerchantPolicy.SourceKey,
            listing.ExternalListingId, listing.RetailerSku, listing.OriginalTitle, listing.ProductUrl, listing.ApprovedAffiliateDestinationReference,
            listing.Seller, listing.IsMarketplaceSeller, listing.Condition.ToString().ToUpperInvariant(), listing.PackQuantity, listing.BundleContents,
            listing.RegionAvailabilityContext, listing.OnlineAvailability.ToString().ToUpperInvariant(), listing.ShippingContext, listing.ExternalIdentifiers,
            listing.SourceObservedAt, listing.FetchedAt, listing.CurrentPriceAmount, listing.CurrentPriceCurrency ?? "CAD", listing.MatchState.ToString().ToUpperInvariant(),
            listing.Evidence.ToString().ToUpperInvariant(), listing.History.ToString().ToUpperInvariant(), listing.IsEnabled, publicEligible, readiness,
            $"/products/{listing.Product.Slug}");
    }

    private static AdminBannerResponse ToBannerResponse(Retailer retailer, StoreBannerProfile? profile, DateTimeOffset now)
    {
        var visibility = profile is null ? "NOT_CONFIGURED" : !profile.IsEnabled ? "DISABLED" : profile.ExpiresAt <= now ? "EXPIRED" :
            profile.EffectiveAt > now ? "SCHEDULED" : profile.CanUseConfiguredAsset(now) ? "ENABLED" : "BLOCKED";
        var rights = profile is null ? "NOT_CONFIGURED" : profile.CanUseConfiguredAsset(now) ? "READY" : "BLOCKED";
        return new AdminBannerResponse(retailer.Id, retailer.Key, retailer.Name, profile?.Id, profile?.Title ?? $"Shop {retailer.Name}",
            profile?.Subtitle ?? "Browse current offers", profile?.AssetPath, profile?.AssetSource.ToString().ToUpperInvariant() ?? "CANADADEALSORIGINAL",
            profile?.BrandAssetPolicy.ToString().ToUpperInvariant() ?? "UNKNOWN", profile?.AssetProvider?.ToString().ToUpperInvariant(),
            profile?.AllowedPlacement, profile?.BannerOrder ?? int.MaxValue, profile?.IsEnabled ?? false, profile?.AssetEvidenceReference,
            profile?.EffectiveAt, profile?.ExpiresAt, visibility, rights);
    }

    private AdminAuditEvent Audit(string action, string entityType, Guid entityId, string summary) =>
        AdminAuditEvent.Create(ActorId(), action, entityType, entityId, summary, clock.GetUtcNow());

    private Guid ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("The owner admin user ID claim is missing.");

    private void ValidateHttpsUrl(string? value, string field, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) ModelState.AddModelError(field, "An HTTPS URL is required.");
            return;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            ModelState.AddModelError(field, "Use an absolute HTTPS URL without embedded credentials.");
    }

    private void ValidateDictionary(IReadOnlyDictionary<string, string>? values, string field)
    {
        if (values is null) return;
        if (values.Count > 40 || values.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 80 || item.Value.Length > 300))
            ModelState.AddModelError(field, "Use at most 40 key/value entries; keys are limited to 80 and values to 300 characters.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record OfferValidationContext(
        Brand Brand,
        Category Category,
        Retailer Retailer,
        MerchantPolicy Policy,
        ProductCondition Condition,
        OnlineAvailabilityState Availability,
        MatchState MatchState);
}
