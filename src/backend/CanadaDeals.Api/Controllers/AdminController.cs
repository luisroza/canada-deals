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
        var managedCategories = await db.Categories.AsNoTracking().OrderBy(category => category.Name)
            .Select(category => new AdminCategoryManagementResponse(
                category.Id,
                category.Name,
                category.Slug,
                category.IsEnabled,
                db.Products.Count(product => product.CategoryId == category.Id),
                category.IsEnabled ? db.RetailerListings.Count(listing => listing.Product.CategoryId == category.Id && listing.IsEnabled &&
                    listing.Retailer.IsEnabled && listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution) : 0))
            .ToListAsync(cancellationToken);
        var managedRetailers = await db.Retailers.AsNoTracking().OrderBy(retailer => retailer.Name)
            .Select(retailer => new AdminRetailerManagementResponse(
                retailer.Id,
                retailer.Name,
                retailer.Key,
                retailer.CountryCode,
                retailer.IsEnabled,
                db.RetailerListings.Count(listing => listing.RetailerId == retailer.Id),
                retailer.IsEnabled ? db.RetailerListings.Count(listing => listing.RetailerId == retailer.Id && listing.IsEnabled &&
                    listing.Product.Category.IsEnabled && listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution) : 0,
                db.StoreBannerProfiles.Any(profile => profile.RetailerId == retailer.Id),
                db.StoreBannerProfiles.Any(profile => profile.RetailerId == retailer.Id && profile.IsEnabled),
                db.AffiliatePrograms.Count(program => program.RetailerId == retailer.Id)))
            .ToListAsync(cancellationToken);
        var profiles = await db.StoreBannerProfiles.AsNoTracking().ToDictionaryAsync(profile => profile.RetailerId, cancellationToken);
        var bannerAssetRows = await db.StoreBannerAssets.AsNoTracking()
            .OrderByDescending(asset => asset.CreatedAt)
            .Select(asset => new { asset.Id, asset.FileName, asset.ContentType, SizeBytes = asset.Content.Length, asset.CreatedAt })
            .ToListAsync(cancellationToken);
        var bannerAssets = bannerAssetRows.Select(asset => new AdminBannerAssetResponse(
            asset.Id, asset.FileName, asset.ContentType, asset.SizeBytes,
            $"{StoreBannerAsset.PublicPathPrefix}{asset.Id:D}", asset.CreatedAt)).ToList();
        var reports = await db.ListingIssueReports.AsNoTracking()
            .Include(report => report.RetailerListing).ThenInclude(listing => listing.Retailer)
            .OrderBy(report => report.Status)
            .ThenByDescending(report => report.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var audit = await db.AdminAuditEvents.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(30).ToListAsync(cancellationToken);

        var offerResponses = offers.Select(ToOfferResponse).ToList();
        var publicCatalogRetailerIds = await db.RetailerListings.AsNoTracking()
            .Where(listing => listing.IsEnabled && listing.Retailer.IsEnabled && listing.Product.Category.IsEnabled &&
                              listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                              listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution)
            .Select(listing => listing.RetailerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var publicCatalogRetailerSet = publicCatalogRetailerIds.ToHashSet();
        var bannerResponses = retailers
            .Select(retailer => ToBannerResponse(retailer, profiles.GetValueOrDefault(retailer.Id), now, publicCatalogRetailerSet.Contains(retailer.Id)))
            .ToList();
        var publicPositions = bannerResponses.Where(banner => banner.IsInPublicCarousel)
            .OrderBy(banner => banner.BannerOrder).ThenBy(banner => banner.Retailer, StringComparer.OrdinalIgnoreCase)
            .Select((banner, index) => new { banner.RetailerId, Position = index + 1 })
            .ToDictionary(item => item.RetailerId, item => item.Position);
        bannerResponses = bannerResponses.Select(banner => banner with
        {
            PublicPosition = publicPositions.GetValueOrDefault(banner.RetailerId, 0) is var position && position > 0 ? position : null
        }).ToList();
        var counts = new AdminDashboardCounts(
            offerResponses.Count(offer => offer.IsEnabled),
            offerResponses.Count(offer => !offer.IsEnabled),
            bannerResponses.Count(banner => banner.IsInPublicCarousel),
            bannerResponses.Count(banner => banner.IsEnabled && (!banner.IsInPublicCarousel || banner.PublicArtworkState == "FALLBACK")),
            reports.Count(report => report.Status == ListingIssueStatus.Open));

        return Ok(new AdminDashboardResponse(
            counts,
            await db.Brands.AsNoTracking().OrderBy(brand => brand.Name).Select(brand => new AdminReferenceOption(brand.Id, brand.Slug, brand.Name)).ToListAsync(cancellationToken),
            managedCategories.Select(category => new AdminReferenceOption(category.Id, category.Slug, category.Name, category.IsEnabled)).ToList(),
            retailers.Select(retailer => new AdminReferenceOption(retailer.Id, retailer.Key, retailer.Name, retailer.IsEnabled)).ToList(),
            managedCategories,
            managedRetailers,
            await db.MerchantPolicies.AsNoTracking().OrderBy(policy => policy.SourceKey).Select(policy => new AdminPolicyOption(
                policy.Id, policy.SourceKey, policy.AllowPriceStorage.ToString().ToUpper(), policy.AllowPriceHistory.ToString().ToUpper(),
                policy.AllowAffiliateLinks.ToString().ToUpper(), policy.RequiredAttribution)).ToListAsync(cancellationToken),
            offerResponses,
            bannerAssets,
            bannerResponses,
            reports.Select(report => new AdminReportResponse(
                report.Id, report.RetailerListingId, report.RetailerListing.Retailer.Name, report.RetailerListing.OriginalTitle,
                report.Reason.ToContract(), report.Note, report.Status.ToContract(), report.CreatedAt, report.UpdatedAt)).ToList(),
            audit.Select(item => new AdminAuditResponse(item.Id, item.Action, item.EntityType, item.EntityId, item.Summary, item.CreatedAt)).ToList()));
    }

    [HttpPost("categories")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateAdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug?.Trim() ?? string.Empty;
        if (!SlugPattern.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(request.Slug), "Use lowercase letters, numbers, and single hyphens.");
            return ValidationProblem(ModelState);
        }

        Category category;
        try { category = Category.Create(request.Name, slug, enabled: false); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.Categories.Add(category);
        db.AdminAuditEvents.Add(Audit("CREATE", "Category", category.Id, "Created an inactive category. Slug is immutable."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin category creation conflicted for slug {Slug}.", slug);
            return Conflict(new ProblemDetails { Title = "Category already exists", Detail = "The category slug is already in use." });
        }

        return Created($"/api/v1/admin/categories/{category.Id}", new { categoryId = category.Id });
    }

    [HttpPut("categories/{categoryId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, UpdateAdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null) return NotFound();
        if (category.IsEnabled && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating a category.");
            return ValidationProblem(ModelState);
        }

        var nameChanged = !string.Equals(category.Name, request.Name?.Trim(), StringComparison.Ordinal);
        try { category.UpdateAdministrativeName(request.Name ?? string.Empty); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }
        var statusChanged = category.IsEnabled != request.IsEnabled;
        category.SetEnabled(request.IsEnabled);

        if (nameChanged)
        {
            var products = await db.Products.Include(product => product.Brand).Include(product => product.Category)
                .Where(product => product.CategoryId == categoryId).ToListAsync(cancellationToken);
            foreach (var product in products) product.RefreshSearchDocument();
        }

        db.AdminAuditEvents.Add(Audit(statusChanged ? (request.IsEnabled ? "ACTIVATE" : "DEACTIVATE") : "UPDATE", "Category", category.Id,
            $"Updated category '{category.Slug}'. Reason: {Normalize(request.ChangeReason) ?? "Routine catalog update"}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("retailers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRetailer(CreateAdminRetailerRequest request, CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        if (!SlugPattern.IsMatch(key))
        {
            ModelState.AddModelError(nameof(request.Key), "Use lowercase letters, numbers, and single hyphens.");
            return ValidationProblem(ModelState);
        }

        Retailer retailer;
        try { retailer = Retailer.Create(key, request.Name, enabled: false); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.Retailers.Add(retailer);
        db.AdminAuditEvents.Add(Audit("CREATE", "Retailer", retailer.Id, "Created an inactive Canadian store. Store key is immutable."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin retailer creation conflicted for key {Key}.", key);
            return Conflict(new ProblemDetails { Title = "Store already exists", Detail = "The store key is already in use." });
        }

        return Created($"/api/v1/admin/retailers/{retailer.Id}", new { retailerId = retailer.Id });
    }

    [HttpPut("retailers/{retailerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRetailer(Guid retailerId, UpdateAdminRetailerRequest request, CancellationToken cancellationToken)
    {
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == retailerId, cancellationToken);
        if (retailer is null) return NotFound();
        if (retailer.IsEnabled && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating a store.");
            return ValidationProblem(ModelState);
        }

        try { retailer.UpdateAdministrativeName(request.Name ?? string.Empty); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }
        var statusChanged = retailer.IsEnabled != request.IsEnabled;
        retailer.SetEnabled(request.IsEnabled);
        db.AdminAuditEvents.Add(Audit(statusChanged ? (request.IsEnabled ? "ACTIVATE" : "DEACTIVATE") : "UPDATE", "Retailer", retailer.Id,
            $"Updated store '{retailer.Key}'. Reason: {Normalize(request.ChangeReason) ?? "Routine store update"}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
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

    [HttpPut("banners/selection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBannerSelection(UpdateAdminBannerSelectionRequest request, CancellationToken cancellationToken)
    {
        var activeRetailerIds = (request.ActiveRetailerIds ?? []).Distinct().ToHashSet();
        var profiles = await db.StoreBannerProfiles.ToListAsync(cancellationToken);
        var profileRetailerIds = profiles.Select(profile => profile.RetailerId).ToHashSet();
        if (!activeRetailerIds.IsSubsetOf(profileRetailerIds))
        {
            ModelState.AddModelError(nameof(request.ActiveRetailerIds), "Configure a banner before activating it in the homepage carousel.");
            return ValidationProblem(ModelState);
        }

        var changes = profiles.Where(profile => profile.IsEnabled != activeRetailerIds.Contains(profile.RetailerId)).ToList();
        if (changes.Any(profile => profile.IsEnabled) && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when removing banners from the homepage carousel.");
            return ValidationProblem(ModelState);
        }

        foreach (var profile in changes)
        {
            var enabled = activeRetailerIds.Contains(profile.RetailerId);
            profile.SetEnabled(enabled);
            db.AdminAuditEvents.Add(Audit(enabled ? "ACTIVATE" : "DEACTIVATE", "StoreBannerProfile", profile.Id,
                $"{(enabled ? "Added banner to" : "Removed banner from")} the homepage carousel. Reason: {Normalize(request.ChangeReason) ?? "Carousel selection update"}."));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} changed {Count} homepage carousel selections.", ActorId(), changes.Count);
        return NoContent();
    }

    [HttpPut("banners/{retailerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertBanner(Guid retailerId, UpsertAdminBannerRequest request, CancellationToken cancellationToken)
    {
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == retailerId, cancellationToken);
        if (retailer is null) return NotFound();
        if (request.AssetPath?.StartsWith(StoreBannerAsset.PublicPathPrefix, StringComparison.Ordinal) == true)
        {
            var idText = request.AssetPath[StoreBannerAsset.PublicPathPrefix.Length..];
            if (!Guid.TryParse(idText, out var assetId) ||
                !await db.StoreBannerAssets.AsNoTracking().AnyAsync(asset => asset.Id == assetId, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.AssetPath), "Choose an uploaded reviewed banner asset.");
                return ValidationProblem(ModelState);
            }
        }
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

    [HttpPost("banner-assets")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(StoreBannerAsset.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = StoreBannerAsset.MaxBytes + 65536)]
    public async Task<ActionResult<AdminBannerAssetResponse>> UploadBannerAsset(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is <= 0 or > StoreBannerAsset.MaxBytes)
        {
            ModelState.AddModelError(nameof(file), "Choose a PNG, JPEG, or WebP image no larger than 2 MB.");
            return ValidationProblem(ModelState);
        }

        var fileName = Path.GetFileName(file.FileName).Trim();
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > StoreBannerAsset.MaxFileNameLength)
        {
            ModelState.AddModelError(nameof(file), $"Use a file name of at most {StoreBannerAsset.MaxFileNameLength} characters.");
            return ValidationProblem(ModelState);
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var bytes = stream.ToArray();
        var contentType = file.ContentType.Trim().ToLowerInvariant();
        if (!IsSupportedBannerImage(contentType, bytes))
        {
            ModelState.AddModelError(nameof(file), "The uploaded bytes must be a valid PNG, JPEG, or WebP image.");
            return ValidationProblem(ModelState);
        }

        var asset = StoreBannerAsset.Create(fileName, contentType, bytes, ActorId(), clock.GetUtcNow());
        db.StoreBannerAssets.Add(asset);
        db.AdminAuditEvents.Add(Audit("UPLOAD", "StoreBannerAsset", asset.Id,
            $"Uploaded reviewed banner image {asset.FileName} ({asset.ContentType}, {asset.Content.Length} bytes)."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} uploaded StoreBannerAsset {AssetId}.", ActorId(), asset.Id);

        var response = new AdminBannerAssetResponse(asset.Id, asset.FileName, asset.ContentType, asset.Content.Length, asset.PublicPath, asset.CreatedAt);
        return Created(asset.PublicPath, response);
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
        if (category is null || !category.IsEnabled) ModelState.AddModelError(nameof(request.CategoryId), "Choose an enabled category.");
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
        var publicEligible = listing.IsEnabled && listing.Retailer.IsEnabled && listing.Product.Category.IsEnabled && listing.MerchantPolicy.CanPublishCurrentPrice &&
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

    private static AdminBannerResponse ToBannerResponse(Retailer retailer, StoreBannerProfile? profile, DateTimeOffset now, bool hasPublicCatalogOffer)
    {
        var visibility = profile is null ? "NOT_CONFIGURED" : !profile.IsEnabled ? "DISABLED" : profile.ExpiresAt <= now ? "EXPIRED" :
            profile.EffectiveAt > now ? "SCHEDULED" : profile.CanUseConfiguredAsset(now) ? "ENABLED" : "BLOCKED";
        var rights = profile is null ? "NOT_CONFIGURED" : profile.CanUseConfiguredAsset(now) ? "READY" : "BLOCKED";
        var isInPublicCarousel = retailer.IsEnabled && profile?.IsEnabled == true && hasPublicCatalogOffer;
        var artworkState = profile?.CanUseConfiguredAsset(now) == true ? "CONFIGURED" : "FALLBACK";
        var eligibilityReason = profile is null ? "Create and activate a banner profile." : !profile.IsEnabled ? "Banner is inactive." :
            !retailer.IsEnabled ? "Retailer is inactive." : !hasPublicCatalogOffer ? "No publicly eligible offer exists for this retailer." :
            artworkState == "FALLBACK" ? "Active with fallback GreatDeals artwork until the selected asset is ready." : "Active in the homepage carousel.";
        return new AdminBannerResponse(retailer.Id, retailer.Key, retailer.Name, profile?.Id, profile?.Title ?? $"Shop {retailer.Name}",
            profile?.Subtitle ?? "Browse current offers", profile?.AssetPath, profile?.AssetSource.ToString().ToUpperInvariant() ?? "CANADADEALSORIGINAL",
            profile?.BrandAssetPolicy.ToString().ToUpperInvariant() ?? "UNKNOWN", profile?.AssetProvider?.ToString().ToUpperInvariant(),
            profile?.AllowedPlacement, profile?.BannerOrder ?? int.MaxValue, profile?.IsEnabled ?? false, profile?.AssetEvidenceReference,
            profile?.EffectiveAt, profile?.ExpiresAt, visibility, rights, isInPublicCarousel, null, artworkState, eligibilityReason);
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

    private static bool IsSupportedBannerImage(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private sealed record OfferValidationContext(
        Brand Brand,
        Category Category,
        Retailer Retailer,
        MerchantPolicy Policy,
        ProductCondition Condition,
        OnlineAvailabilityState Availability,
        MatchState MatchState);
}
